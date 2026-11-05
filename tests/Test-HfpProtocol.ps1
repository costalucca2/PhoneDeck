param([string]$Executable=(Join-Path $PSScriptRoot '..\out\PhoneDeck.exe'))
$ErrorActionPreference='Stop'
$fixture=@"
public class FakeAtChannel:PhoneDeckNative.IAtChannel {
 public System.Collections.Generic.Queue<string> Replies=new System.Collections.Generic.Queue<string>();
 public System.Collections.Generic.List<string> Commands=new System.Collections.Generic.List<string>();
 public string Execute(string command){Commands.Add(command);if(Replies.Count==0)throw new System.Exception("Unexpected command: "+command);return Replies.Dequeue();}
}
"@
$binary=(Resolve-Path $Executable).Path
[void][Reflection.Assembly]::LoadFrom($binary)
$fixtureSource=Join-Path (Split-Path $binary -Parent) 'hfp-test-fixture.local.cs'
$fixtureBinary=Join-Path (Split-Path $binary -Parent) 'hfp-test-fixture.local.dll'
[IO.File]::WriteAllText($fixtureSource,$fixture)
& (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe') /nologo /target:library ('/reference:'+$binary) ('/out:'+$fixtureBinary) $fixtureSource
if($LASTEXITCODE){throw 'Fixture compilation failed'}
[void][Reflection.Assembly]::LoadFrom($fixtureBinary)
function Assert($condition,$message){if(-not $condition){throw $message}}
function Session {
 $channel=New-Object FakeAtChannel
 foreach($reply in @("+BRSF: 0`r`nOK`r`n",('+CIND: ("callsetup",(0-3)),("call",(0,1)),("callheld",(0-2))'+"`r`nOK`r`n"),"+CIND: 0,0,0`r`nOK`r`n","OK`r`n")){$channel.Replies.Enqueue($reply)}
 $protocol=New-Object PhoneDeckNative.HfpProtocol $channel
 [void]$protocol.Initialize()
 return @{Channel=$channel;Protocol=$protocol}
}
$s=Session;Assert $s.Protocol.Ready 'Handshake failed'
Assert ($s.Channel.Commands[0] -eq 'AT+BRSF=32') 'Advertised unsupported features'
$s.Channel.Replies.Enqueue("+CIND: 0,0,0`r`nOK`r`n");$s.Channel.Replies.Enqueue("OK`r`n");$s.Protocol.Dial('00000000');Assert ($s.Channel.Commands[$s.Channel.Commands.Count-1] -eq 'ATD00000000;') 'Dial command failed'
$count=$s.Channel.Commands.Count;try{$s.Protocol.Dial("00000000;`rATA");throw 'Injection accepted'}catch{Assert ($_.Exception.Message -ne 'Injection accepted') 'Injection accepted'};Assert ($s.Channel.Commands.Count -eq $count) 'Invalid number touched transport'
$s=Session;$s.Channel.Replies.Enqueue("+CIND: 1,0,0`r`nOK`r`n");$s.Channel.Replies.Enqueue("OK`r`n");$s.Protocol.Answer();Assert ($s.Channel.Commands[$s.Channel.Commands.Count-1] -eq 'ATA') 'Answer failed with reordered indicators'
$s=Session;$s.Channel.Replies.Enqueue("+CIND: 0,1,0`r`nOK`r`n");try{$s.Protocol.Dial('00000000');throw 'Busy dial accepted'}catch{Assert ($_.Exception.Message -ne 'Busy dial accepted') 'Busy dial accepted'};Assert (-not ($s.Channel.Commands -match '^ATD')) 'Busy phone was dialed'
$s=Session;$s.Channel.Replies.Enqueue("+CIND: 0,1,0`r`nOK`r`n");$s.Channel.Replies.Enqueue("+CLCC: 1,0,0,0,0`r`nOK`r`n");$s.Channel.Replies.Enqueue("OK`r`n");$s.Protocol.Hangup();Assert ($s.Channel.Commands[$s.Channel.Commands.Count-1] -eq 'AT+CHUP') 'Hangup failed'
$s=Session;$s.Channel.Replies.Enqueue("+CIND: 0,1,1`r`nOK`r`n");$s.Channel.Replies.Enqueue("+CLCC: 1,0,0,0,0`r`n+CLCC: 2,0,1,0,0`r`nOK`r`n");try{$s.Protocol.Hangup();throw 'Multiple calls accepted'}catch{Assert ($_.Exception.Message -ne 'Multiple calls accepted') 'Multiple calls accepted'};Assert (-not ($s.Channel.Commands -contains 'AT+CHUP')) 'Multiple calls were changed'
$s=Session;$s.Channel.Replies.Enqueue("+CIND: 0,0,0`r`nOK`r`n");$s.Channel.Replies.Enqueue("ERROR`r`n");try{$s.Protocol.Dial('00000000');throw 'Error accepted'}catch{Assert ($_.Exception.Message -ne 'Error accepted') 'Error accepted'};Assert (@($s.Channel.Commands|Where-Object {$_ -like 'ATD*'}).Count -eq 1) 'Dial automatically retried'
$descriptor=[byte[]](0x35,0x0c,0x35,3,0x19,1,0,0x35,5,0x19,0,3,8,8)
Assert ([PhoneDeckNative.HfpSdp]::RfcommChannel($descriptor) -eq 8) 'Real SDP channel descriptor failed'
foreach($bad in @([byte[]]@(0x19,0,3,8,0),[byte[]]@(0x19,0,3,8,8,0x19,0,3,8,9),[byte[]]@(0x35,0))){try{[void][PhoneDeckNative.HfpSdp]::RfcommChannel($bad);throw 'Invalid SDP accepted'}catch{Assert ($_.Exception.Message -ne 'Invalid SDP accepted') 'Invalid SDP accepted'}}
'PASS: HFP handshake, reordered indicators, dial/answer/hangup, injection rejection, busy/multiple-call guards, ERROR handling and no retry. Fake transport only.'# Exercise the same dispatch used by the live named-pipe service; fake phone only.
$s=Session;$dispatch=New-Object PhoneDeckNative.HfpCommandDispatch $s.Protocol
$s.Channel.Replies.Enqueue("+CIND: 0,0,0`r`nOK`r`n");[void]$dispatch.Execute('status','');Assert (-not ($s.Channel.Commands -match '^(ATD|ATA$|AT\+CHUP$)')) 'Status sent a call command'
$s.Channel.Replies.Enqueue("+CIND: 0,0,0`r`nOK`r`n");$s.Channel.Replies.Enqueue("OK`r`n");[void]$dispatch.Execute('dial','00000000');Assert (@($s.Channel.Commands|Where-Object {$_ -eq 'ATD00000000;'}).Count -eq 1) 'Dispatch did not dial exactly once'
$s=Session;$dispatch=New-Object PhoneDeckNative.HfpCommandDispatch $s.Protocol
$s.Channel.Replies.Enqueue("+CIND: 1,0,0`r`nOK`r`n");$s.Channel.Replies.Enqueue("OK`r`n");[void]$dispatch.Execute('answer','');Assert (@($s.Channel.Commands|Where-Object {$_ -eq 'ATA'}).Count -eq 1) 'Dispatch did not answer exactly once'
$s=Session;$dispatch=New-Object PhoneDeckNative.HfpCommandDispatch $s.Protocol
$s.Channel.Replies.Enqueue("+CIND: 0,1,0`r`nOK`r`n");$s.Channel.Replies.Enqueue("+CLCC: 1,0,0,0,0`r`nOK`r`n");$s.Channel.Replies.Enqueue("OK`r`n");[void]$dispatch.Execute('hangup','');Assert (@($s.Channel.Commands|Where-Object {$_ -eq 'AT+CHUP'}).Count -eq 1) 'Dispatch did not hang up exactly once'
$count=$s.Channel.Commands.Count;try{$dispatch.Execute('invalid','');throw 'Unknown command accepted'}catch{Assert ($_.Exception.Message -ne 'Unknown command accepted') 'Unknown command accepted'};Assert ($s.Channel.Commands.Count -eq $count) 'Unknown action touched phone'
$s=Session;$dispatch=New-Object PhoneDeckNative.HfpCommandDispatch $s.Protocol
$s.Channel.Replies.Enqueue("+CIND: 0,0,0`r`nOK`r`n");try{$dispatch.Execute('answer','');throw 'Idle answer accepted'}catch{Assert ($_.Exception.Message -ne 'Idle answer accepted') 'Idle answer accepted'};Assert (-not ($s.Channel.Commands -contains 'ATA')) 'Idle dispatch sent answer'
$s=Session;$dispatch=New-Object PhoneDeckNative.HfpCommandDispatch $s.Protocol
$s.Channel.Replies.Enqueue("+CIND: 0,0,0`r`nOK`r`n");try{$dispatch.Execute('hangup','');throw 'Idle hangup accepted'}catch{Assert ($_.Exception.Message -ne 'Idle hangup accepted') 'Idle hangup accepted'};Assert (-not ($s.Channel.Commands -contains 'AT+CHUP')) 'Idle dispatch sent hangup'
'PASS: production dispatch routes status/dial/answer/hangup exactly once; rejects unknown commands, idle answer and idle hangup. Simulated phone only.'
$channel=New-Object FakeAtChannel
$past=[DateTime]::UtcNow.AddSeconds(-1)
$expired=New-Object PhoneDeckNative.DeadlineAtChannel $channel,$past
foreach($command in @('ATD00000000;','ATA','AT+CHUP','AT+CIND?')){try{$expired.Execute($command);throw 'Expired instruction sent'}catch{Assert ($_.Exception.Message -ne 'Expired instruction sent') 'Expired instruction sent'}}
Assert ($channel.Commands.Count -eq 0) 'Expired command reached phone'
$future=[DateTime]::UtcNow.AddMinutes(1)
$current=New-Object PhoneDeckNative.DeadlineAtChannel $channel,$future
$channel.Replies.Enqueue("OK`r`n");[void]$current.Execute('AT+CIND?');Assert ($channel.Commands.Count -eq 1) 'Unexpired read blocked'
$current.DeadlineUtc=$past;try{$current.Execute('ATA');throw 'Late answer sent'}catch{Assert ($_.Exception.Message -ne 'Late answer sent') 'Late answer sent'};Assert ($channel.Commands.Count -eq 1) 'Later expiry did not stop instruction'
'PASS: request deadline prevents late dial/answer/hangup/read before touching transport; expiration between instructions is respected. Fake phone only.'
