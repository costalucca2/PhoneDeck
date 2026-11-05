param([string]$OutputDirectory=(Join-Path $PSScriptRoot '..\out'))
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$framework=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
New-Item -ItemType Directory -Path $OutputDirectory -Force|Out-Null
$target=Join-Path $OutputDirectory 'PhoneDeck.exe'
$qrRoot=Join-Path $OutputDirectory 'qrcoder'
$qrDll=Join-Path $qrRoot 'package\lib\net40\QRCoder.dll'
if(-not (Test-Path $qrDll)){
 New-Item -ItemType Directory $qrRoot -Force|Out-Null
 $archive=Join-Path $qrRoot 'qrcoder.zip'
 Invoke-WebRequest 'https://api.nuget.org/v3-flatcontainer/qrcoder/1.8.1/qrcoder.1.8.1.nupkg' -OutFile $archive -UseBasicParsing
 if((Get-FileHash $archive).Hash -ne 'D3FE5C80C742B6A2EF8A31734C06600E2D674806A97F36F4AF97B0D2871DE309'){throw 'QRCoder package hash mismatch'}
 Expand-Archive -LiteralPath $archive -DestinationPath (Join-Path $qrRoot 'package') -Force
}
$refs=@('System.Windows.Forms.dll','System.Drawing.dll','System.Web.Extensions.dll',(Join-Path $framework 'System.Runtime.dll'),(Join-Path $framework 'WPF\UIAutomationClient.dll'),(Join-Path $framework 'WPF\UIAutomationTypes.dll'),(Join-Path $framework 'WPF\WindowsBase.dll'),(Join-Path $env:WINDIR 'System32\WinMetadata\Windows.Foundation.winmd'),(Join-Path $env:WINDIR 'System32\WinMetadata\Windows.System.winmd'))
$refs+=(Join-Path $framework 'System.Runtime.InteropServices.WindowsRuntime.dll')
$refs+=@('ApplicationModel','Devices','Networking','Storage'|ForEach-Object {Join-Path $env:WINDIR ('System32\WinMetadata\Windows.'+$_+'.winmd')})
$parameters=@('/nologo','/target:winexe','/platform:x64','/optimize+',('/win32manifest:'+(Join-Path $root 'src\app.manifest')),('/out:'+$target))
$parameters+=@(('/win32icon:'+(Join-Path $root 'assets\PhoneDeck.ico')))
$parameters+=@($refs|ForEach-Object {'/reference:'+$_})
$parameters+=@(('/reference:'+$qrDll),('/resource:'+$qrDll+',PhoneDeck.QRCoder.dll'))
$parameters+=@(Get-ChildItem (Join-Path $root 'src') -Filter '*.cs'|ForEach-Object {$_.FullName})
& (Join-Path $framework 'csc.exe') @parameters
if($LASTEXITCODE){throw 'Compilation failed'}
[pscustomobject]@{Version=(Get-Item $target).VersionInfo.FileVersion;ExecutableHash=(Get-FileHash $target).Hash;Sources=@(Get-ChildItem (Join-Path $root 'src') -File|Get-FileHash|Select-Object @{n='File';e={Split-Path $_.Path -Leaf}},Hash)}|ConvertTo-Json -Depth 6|Set-Content (Join-Path $OutputDirectory 'build.json') -Encoding UTF8
Write-Output $target
