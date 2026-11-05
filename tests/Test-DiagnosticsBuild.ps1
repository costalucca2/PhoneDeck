$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$output=Join-Path $root 'out\diagnostics'
New-Item -ItemType Directory -Path $output -Force|Out-Null
$framework=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$references=@('System.Windows.Forms.dll',(Join-Path $framework 'System.Runtime.dll'),(Join-Path $framework 'System.Runtime.InteropServices.WindowsRuntime.dll'))
$references+=@('Foundation','ApplicationModel','Devices','Networking','Storage','Security'|ForEach-Object {Join-Path $env:WINDIR ('System32\WinMetadata\Windows.'+$_+'.winmd')})
$names=@('AudioPinProbe','AudioStreamProbe','ClassicBluetoothStatus','EnableIphoneHfp','HfpProbe','NativeAccess','NativeLines','NativeProbe','RawHfpProbe','TapiProbe','NativeAudioAccess')
foreach($name in $names){
 $parameters=@('/nologo','/target:exe','/platform:x64',('/out:'+(Join-Path $output ($name+'.exe'))))
 $parameters+=@($references|ForEach-Object {'/reference:'+$_})
 if($name -eq 'NativeAudioAccess'){$parameters+=@('/define:NATIVE_AUDIO_PACKAGE','/main:NativeAudioAccess',(Join-Path $root 'tools\native-probe\AudioStreamProbe.cs'))}
 if($name -eq 'RawHfpProbe'){$parameters+=(Join-Path $root 'src\HfpProtocol.cs')}
 $parameters+=(Join-Path $root ('tools\native-probe\'+$name+'.cs'))
 & (Join-Path $framework 'csc.exe') @parameters
 if($LASTEXITCODE){throw ($name+' failed to compile')}
}
'PASS: all native diagnostic sources compile without generated private files. No diagnostic executed, no driver registration or phone access.'
