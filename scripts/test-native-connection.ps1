param([ValidateSet('Hfp','Tapi','RawHfp')][string]$Transport='Hfp',[switch]$Cached,[switch]$Status)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$out=Join-Path $root 'out'
$framework=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
New-Item -ItemType Directory $out -Force|Out-Null
$name=switch($Transport){'Hfp'{'HfpProbe'} 'RawHfp'{'RawHfpProbe'} 'Tapi'{'TapiProbe'}}
$target=Join-Path $out ($name+'.exe')
$parameters=@('/nologo','/target:exe','/platform:x64',('/out:'+$target))
if($Transport -ne 'Tapi'){
 $references=@((Join-Path $framework 'System.Runtime.dll'),(Join-Path $framework 'System.Runtime.InteropServices.WindowsRuntime.dll'))+@('Foundation','ApplicationModel','Devices','Networking','Storage'|ForEach-Object {Join-Path $env:WINDIR ('System32\WinMetadata\Windows.'+$_+'.winmd')})
 $parameters+=@($references|ForEach-Object {'/reference:'+$_})
}
$source=Join-Path $root ('tools\native-probe\'+$name+'.cs')
$parameters+=$source
if($Transport -eq 'RawHfp'){$parameters+=(Join-Path $root 'src\HfpProtocol.cs')}
& (Join-Path $framework 'csc.exe') @parameters
if($LASTEXITCODE){throw 'Native diagnostic compilation failed'}
$sourceFiles=@($source)
if($Transport -eq 'RawHfp'){$sourceFiles+=(Join-Path $root 'src\HfpProtocol.cs')}
$identity=[pscustomobject]@{Sources=@($sourceFiles|Get-FileHash|Select-Object @{n='File';e={Split-Path $_.Path -Leaf}},Hash);Tool=$name;Version=(Get-Item $target).VersionInfo.FileVersion;SourceHash=(Get-FileHash $source).Hash;ExecutableHash=(Get-FileHash $target).Hash;CallsAllowed=$false}
$identity|ConvertTo-Json|Set-Content (Join-Path $out ($name+'-build.local.json')) -Encoding UTF8
$process=Start-Process -FilePath $target -ArgumentList $(if($Status){"--status"}elseif($Cached){"--cached"}else{"--fresh"}) -WindowStyle Hidden -PassThru
if(-not $process.WaitForExit(45000)){throw 'Diagnostic still running; no new instance was started'}
$report=switch($Transport){'Hfp'{'hfp-probe.local.txt'} 'RawHfp'{'raw-hfp-probe.local.txt'} 'Tapi'{'tapi-diagnostic.local.txt'}}
Get-Content (Join-Path $out $report)
if($process.ExitCode){Write-Warning 'Native readiness was not confirmed. Installed call backend was not changed.'}