param([string]$Executable=(Join-Path $PSScriptRoot '..\out\PhoneDeck.exe'))
$ErrorActionPreference='Stop'
$binary=(Resolve-Path $Executable).Path
$source=@'
using System;
using System.IO;
using System.Linq; using System.Collections.Generic; using System.Web.Script.Serialization;
using System.Threading;
public static class RecordingTests {
 static void Assert(bool result,string message){if(!result)throw new Exception(message);}
 public static void Run(string folder){
  CaptureAudio fake=(id,flow,sink,ready,stop)=>{ready();var samples=Enumerable.Repeat(flow==1?.2f:.6f,480).ToArray();while(!stop.IsCancellationRequested){sink(samples,48000);Thread.Sleep(10);}};
  var recording=new ConversationRecorder(fake);recording.Start(folder,"fake-input","fake-output",new Dictionary<string,object>{{"Direction","outgoing"},{"ContactName","Synthetic contact"},{"NumberDialed","0211100000000"},{"NetworkIdentityVerified",false},{"MonitorHfp",false}});var until=DateTime.UtcNow.AddSeconds(3);while(!recording.Started&&recording.Recording&&DateTime.UtcNow<until)Thread.Sleep(10);Assert(recording.Started,"Recording did not start");Thread.Sleep(30);Assert(RecordingStatus.Read(folder).IsRecording,"Live recorder status missing");Assert(ConversationRecorder.AnyRecording,"Active recording was not detected");
  var second=new ConversationRecorder(fake);second.Start(folder,"fake-input","fake-output");Thread.Sleep(80);second.Stop().GetAwaiter().GetResult();Assert(!String.IsNullOrEmpty(second.Error),"Concurrent recording was accepted");
  Thread.Sleep(200);recording.Stop().GetAwaiter().GetResult();Assert(String.IsNullOrEmpty(recording.Error),recording.Error);Assert(!ConversationRecorder.AnyRecording,"Recording lock leaked");Assert(RecordingStatus.Read(folder).Phase=="stopped","Recorder status not stopped");var png=UlanziRecorderBridge.Image(new RecorderState{Phase="recording",Seconds=65});Assert(png.StartsWith("data:image/png;base64,"),"Ulanzi image missing");Assert(new RecorderState{Seconds=65}.Elapsed=="01:05","Timer text incorrect");var packet=UlanziRecorderBridge.Packet(UlanziRecorderBridge.PluginId+".screen","0_2","test",new RecorderState{Phase="recording",Seconds=65});Assert((string)packet["cmd"]=="state"&&(string)packet["uuid"]==UlanziRecorderBridge.PluginId,"Ulanzi envelope incorrect");
  byte[] data=File.ReadAllBytes(recording.FilePath);Assert(data.Length>1000,"Recording has no samples");Assert(System.Text.Encoding.ASCII.GetString(data,0,4)=="RIFF"&&System.Text.Encoding.ASCII.GetString(data,8,4)=="WAVE","Invalid WAV signature");Assert(BitConverter.ToInt16(data,22)==1&&BitConverter.ToInt32(data,24)==48000&&BitConverter.ToInt16(data,34)==16,"Invalid WAV format");Assert(BitConverter.ToUInt32(data,40)==data.Length-44,"Final WAV length was not saved");
  bool mixed=false;for(int i=44;i+1<data.Length;i+=2)if(Math.Abs(BitConverter.ToInt16(data,i)-13107)<2)mixed=true;Assert(mixed,"Microphone and playback were not mixed together");
  string bundle=Path.GetDirectoryName(recording.FilePath);Assert(EvidenceJournal.VerifyBundle(bundle),"Evidence bundle invalid");
  var manifest=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(File.ReadAllText(Path.Combine(bundle,"manifesto.json")));
  var context=(Dictionary<string,object>)manifest["Context"];Assert(Convert.ToString(context["NumberDialed"])=="0211100000000","Leading zero lost");Assert(!Convert.ToBoolean(context["NetworkIdentityVerified"]),"Identity falsely certified");Assert(Convert.ToString(manifest["Status"])=="finalized","Final state incorrect");
  foreach(string name in new[]{"conversa.wav","eventos.jsonl","resumo.txt","manifesto.json"}){string path=Path.Combine(bundle,name);byte[] original=File.ReadAllBytes(path);File.AppendAllText(path,"tamper");Assert(!EvidenceJournal.VerifyBundle(bundle),"Alteration undetected: "+name);File.WriteAllBytes(path,original);Assert(EvidenceJournal.VerifyBundle(bundle),"Restored bundle invalid");}
  var rows=File.ReadAllLines(Path.Combine(bundle,"eventos.jsonl"));Assert(rows.Length>=5,"Incomplete event journal");var envelope=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(rows[0]);var first=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(Convert.ToString(envelope["PayloadJson"]));Assert(Convert.ToString(first["Utc"]).EndsWith("Z"),"UTC missing");DateTimeOffset.Parse(Convert.ToString(first["LocalWithOffset"]));Assert(first.ContainsKey("MonotonicTicks")&&first.ContainsKey("TimeZoneId"),"Clock metadata missing");
  var fail=new ConversationRecorder((id,flow,sink,ready,stop)=>{throw new Exception("simulated capture failure");});fail.Start(folder,"fake-input","fake-output");Thread.Sleep(50);fail.Stop().GetAwaiter().GetResult();Assert(fail.Error.Contains("simulated capture failure"),"Capture failure was hidden");Assert(EvidenceJournal.VerifyBundle(Path.GetDirectoryName(fail.FilePath)),"Partial bundle integrity missing");var partial=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(File.ReadAllText(Path.Combine(Path.GetDirectoryName(fail.FilePath),"manifesto.json")));Assert(Convert.ToString(partial["Status"])=="partial","Capture failure marked complete");
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
