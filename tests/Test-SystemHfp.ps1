param([string]$Executable=(Join-Path $PSScriptRoot '..\out\PhoneDeck.exe'))
$ErrorActionPreference='Stop'
$binary=(Resolve-Path $Executable).Path
$source=@'
using System;
using PhoneDeckNative;
public sealed class FakeSystemHfp:ISystemHfp {
 public uint Count;public bool Ring,Busy,AudioError,Slow,ConnectError;
 public int Commands,AudioRequests,MicRequests;public bool MicMuted;public string Last;
 public bool Incoming {get{return Ring;}}public bool Active {get{return Busy;}}
 public void Connect(DateTime d){if(ConnectError)throw new Exception("Connection refused");if(Slow)System.Threading.Thread.Sleep(40);}
 public uint CallCount(){return Count;}
 public void Command(string a,string n,DateTime d){Commands++;Last=a;}
 public void LocalAudio(DateTime d){AudioRequests++;if(AudioError)throw new Exception("Audio refused");}
 public void MicrophoneMute(bool muted,DateTime d){MicRequests++;MicMuted=muted;}
 public void Dispose(){}
}
public static class SystemHfpTests {
 static DateTime Limit {get{return DateTime.UtcNow.AddSeconds(2);}}
 static int assertion; static void Assert(bool ok){assertion++;if(!ok)throw new Exception("Native HFP policy assertion failed #"+assertion);}
 static void Block(FakeSystemHfp f,string action,string number,DateTime limit){bool blocked=false;try{new SystemHfpDispatch(f).Execute(action,number,limit);}catch{blocked=true;}Assert(blocked&&f.Commands==0&&f.AudioRequests==0);}
 public static void Run(){
  foreach(bool muted in new[]{false,true}){var m=new FakeSystemHfp{Count=1,Busy=true};new SystemHfpDispatch(m).Execute(muted?"mic-off":"mic-on",null,Limit);Assert(m.Commands==0&&m.AudioRequests==0&&m.MicRequests==1&&m.MicMuted==muted);}
  var f=new FakeSystemHfp();var d=new SystemHfpDispatch(f);d.Execute("status",null,Limit);Assert(f.Commands==0&&f.AudioRequests==0);
  d.Execute("audio",null,Limit);Assert(f.Commands==0&&f.AudioRequests==1);
  foreach(string action in new[]{"dial","answer","hangup"}){f=new FakeSystemHfp();if(action!="dial")f.Count=1;if(action=="answer")f.Ring=true;new SystemHfpDispatch(f).Execute(action,"00000000",Limit);Assert(f.Commands==1&&f.Last==action&&f.AudioRequests==(action=="hangup"?0:1));}
  foreach(string action in new[]{"dial","answer","hangup","audio"})Block(new FakeSystemHfp{Count=2},action,"00000000",Limit);
  Block(new FakeSystemHfp{Busy=true},"dial","00000000",Limit);
  Block(new FakeSystemHfp{Ring=true},"dial","00000000",Limit);
  Block(new FakeSystemHfp{Count=1},"answer",null,Limit);
  Block(new FakeSystemHfp{Count=1,Ring=true,Busy=true},"answer",null,Limit);
  Block(new FakeSystemHfp(),"hangup",null,Limit);
  Block(new FakeSystemHfp(),"dial","00000000;",Limit);
  Block(new FakeSystemHfp(),"dial","00000000",DateTime.UtcNow.AddSeconds(-1));
  Block(new FakeSystemHfp{Slow=true},"dial","00000000",DateTime.UtcNow.AddMilliseconds(15));
  Block(new FakeSystemHfp{ConnectError=true},"dial","00000000",Limit);
  f=new FakeSystemHfp{AudioError=true};var result=new SystemHfpDispatch(f).Execute("dial","00000000",Limit);Assert(f.Commands==1&&f.AudioRequests==1&&!(bool)result["AudioPrepared"]&&((string)result["Message"]).Contains("repita"));
 }
}
'@
$dir=Split-Path $binary -Parent
$cs=Join-Path $dir 'system-hfp-tests.local.cs'
$dll=Join-Path $dir 'system-hfp-tests.local.dll'
[IO.File]::WriteAllText($cs,$source)
& (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe') /nologo /target:library ('/reference:'+$binary) ('/out:'+$dll) $cs
if($LASTEXITCODE){throw 'Native policy test compilation failed'}
[void][Reflection.Assembly]::LoadFrom($binary)
[void][Reflection.Assembly]::LoadFrom($dll)
[SystemHfpTests]::Run()
'PASS: native dispatch, audio selection, idle/multiple-call/deadline guards, partial audio failure and no retry. Simulated phone only.'
