param([string]$OutputDirectory=(Join-Path $PSScriptRoot '..\out'))
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$framework=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
New-Item -ItemType Directory -Path $OutputDirectory -Force|Out-Null
$target=Join-Path $OutputDirectory 'PhoneDeck.exe'
$refs=@('System.Windows.Forms.dll','System.Drawing.dll','System.Web.Extensions.dll',(Join-Path $framework 'System.Runtime.dll'),(Join-Path $framework 'WPF\UIAutomationClient.dll'),(Join-Path $framework 'WPF\UIAutomationTypes.dll'),(Join-Path $framework 'WPF\WindowsBase.dll'),(Join-Path $env:WINDIR 'System32\WinMetadata\Windows.Foundation.winmd'),(Join-Path $env:WINDIR 'System32\WinMetadata\Windows.System.winmd'))
$parameters=@('/nologo','/target:winexe','/platform:x64','/optimize+',('/win32manifest:'+(Join-Path $root 'src\app.manifest')),('/out:'+$target))
$parameters+=@($refs|ForEach-Object {'/reference:'+$_})
$parameters+=@(Get-ChildItem (Join-Path $root 'src') -Filter '*.cs'|ForEach-Object {$_.FullName})
& (Join-Path $framework 'csc.exe') @parameters
if($LASTEXITCODE){throw 'Compilation failed'}
[pscustomobject]@{Version=(Get-Item $target).VersionInfo.FileVersion;ExecutableHash=(Get-FileHash $target).Hash;Sources=@(Get-ChildItem (Join-Path $root 'src') -File|Get-FileHash|Select-Object @{n='File';e={Split-Path $_.Path -Leaf}},Hash)}|ConvertTo-Json -Depth 6|Set-Content (Join-Path $OutputDirectory 'build.json') -Encoding UTF8
Write-Output $target

