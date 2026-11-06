param([string]$Executable=(Join-Path $PSScriptRoot '..\out\PhoneDeck.exe'))
$ErrorActionPreference='Stop'
$binary=(Resolve-Path $Executable).Path
$source=@'
using System;
using System.IO;
using System.Linq;
using System.Threading;
public static class RecordingTests {
 static void Assert(bool result,string message){if(!result)throw new Exception(message);}
 public static void Run(string folder){
  CaptureAudio fake=(id,flow,sink,ready,stop)=>{ready();var samples=Enumerable.Repeat(flow==1?.2f:.6f,480).ToArray();while(!stop.IsCancellationRequested){sink(samples,48000);Thread.Sleep(10);}};
  var recording=new ConversationRecorder(fake);recording.Start(folder,"fake-input","fake-output");var until=DateTime.UtcNow.AddSeconds(3);while(!recording.Started&&recording.Recording&&DateTime.UtcNow<until)Thread.Sleep(10);Assert(recording.Started,"Recording did not start");Assert(ConversationRecorder.AnyRecording,"Active recording was not detected");
  var second=new ConversationRecorder(fake);second.Start(folder,"fake-input","fake-output");Thread.Sleep(80);second.Stop().GetAwaiter().GetResult();Assert(!String.IsNullOrEmpty(second.Error),"Concurrent recording was accepted");
  Thread.Sleep(200);recording.Stop().GetAwaiter().GetResult();Assert(String.IsNullOrEmpty(recording.Error),recording.Error);Assert(!ConversationRecorder.AnyRecording,"Recording lock leaked");
  byte[] data=File.ReadAllBytes(recording.FilePath);Assert(data.Length>1000,"Recording has no samples");Assert(System.Text.Encoding.ASCII.GetString(data,0,4)=="RIFF"&&System.Text.Encoding.ASCII.GetString(data,8,4)=="WAVE","Invalid WAV signature");Assert(BitConverter.ToInt16(data,22)==1&&BitConverter.ToInt32(data,24)==48000&&BitConverter.ToInt16(data,34)==16,"Invalid WAV format");Assert(BitConverter.ToUInt32(data,40)==data.Length-44,"Final WAV length was not saved");
  bool mixed=false;for(int i=44;i+1<data.Length;i+=2)if(Math.Abs(BitConverter.ToInt16(data,i)-13107)<2)mixed=true;Assert(mixed,"Microphone and playback were not mixed together");
  var fail=new ConversationRecorder((id,flow,sink,ready,stop)=>{throw new Exception("simulated capture failure");});fail.Start(folder,"fake-input","fake-output");Thread.Sleep(50);fail.Stop().GetAwaiter().GetResult();Assert(fail.Error.Contains("simulated capture failure"),"Capture failure was hidden");
 }
}
'@
$dir=Split-Path $binary -Parent
$cs=Join-Path $dir 'recording-tests.local.cs';$dll=Join-Path $dir 'recording-tests.local.dll'
[IO.File]::WriteAllText($cs,$source)
& (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe') /nologo /target:library ('/reference:'+$binary) ('/out:'+$dll) $cs
if($LASTEXITCODE){throw 'Recording fixture compilation failed'}
[void][Reflection.Assembly]::LoadFrom($binary);[void][Reflection.Assembly]::LoadFrom($dll)
$folder=Join-Path ([IO.Path]::GetTempPath()) ('phonedeck-recording-test-'+[guid]::NewGuid().ToString('N'))
try{[RecordingTests]::Run($folder);'PASS: synthetic microphone/playback mix, saved WAV format/length, single recorder lock and capture failure. No microphone, call or real audio recorded.'}
finally{if(Test-Path -LiteralPath $folder){$resolved=[IO.Path]::GetFullPath($folder);if((Split-Path $resolved -Parent).TrimEnd('\') -eq ([IO.Path]::GetTempPath()).TrimEnd('\') -and (Split-Path $resolved -Leaf) -like 'phonedeck-recording-test-*'){Remove-Item -LiteralPath $resolved -Recurse -Force}}}
