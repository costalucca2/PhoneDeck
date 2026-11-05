param([string]$Executable=(Join-Path $PSScriptRoot '..\out\RawHfpProbe.exe'))
$ErrorActionPreference='Stop'
$a=[Reflection.Assembly]::LoadFrom((Resolve-Path $Executable).Path)
$t=$a.GetType('RawHfpProbe').GetNestedType('Endpoint',[Reflection.BindingFlags]::NonPublic)
$endpoint=[Activator]::CreateInstance($t,[Reflection.BindingFlags]'Instance,Public,NonPublic',$null,[object[]]@([UInt64]0x112233445566,[int]8),$null)
$remote=$endpoint.Serialize();$roundtrip=$endpoint.Create($remote).Serialize()
for($i=0;$i -lt 30;$i++){if($remote[$i] -ne $roundtrip[$i]){throw 'Remote endpoint round trip failed'}}
$local=New-Object System.Net.SocketAddress ([Net.Sockets.AddressFamily]32),30
$bound=$endpoint.Create($local).Serialize()
for($i=2;$i -lt 30;$i++){if($bound[$i] -ne 0){throw 'BUG: wildcard local endpoint was replaced with the remote phone address/channel'}}
'PASS: remote endpoint round trip; wildcard local bind preserved. No Bluetooth, network or call action.'