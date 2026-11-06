using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using PhoneDeckAudio;

public delegate void CaptureAudio(string id,int flow,Action<float[],int> sink,Action ready,CancellationToken stop);
public sealed class ConversationRecorder {
 readonly CaptureAudio capture;readonly CancellationTokenSource cancel=new CancellationTokenSource();Task worker;readonly Stopwatch elapsed=new Stopwatch();
 Dictionary<string,object> metadata;string recordingId,stopReason="capture_ended";RecordingEvidence evidence;
 public string FilePath {get;private set;}public string Error {get;private set;}
 public bool Started {get;private set;}
 public bool Finalizing {get;private set;}
 public bool Recording {get{return worker!=null&&!worker.IsCompleted;}}public TimeSpan Duration {get{return elapsed.Elapsed;}}
 public static string Folder {get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"Gravacoes","PhoneDeck");}}
 const string MutexName="Local\\PhoneDeck.ConversationRecording";
 public static bool AnyRecording {get{try{using(var mutex=Mutex.OpenExisting(MutexName)){bool available=false;try{available=mutex.WaitOne(0);return !available;}catch(AbandonedMutexException){available=true;return false;}finally{if(available)mutex.ReleaseMutex();}}}catch(WaitHandleCannotBeOpenedException){return false;}}}
 public ConversationRecorder(CaptureAudio source){capture=source;}
 public void Start(string folder,string input,string output,Dictionary<string,object> context=null){if(worker!=null)throw new InvalidOperationException("Recorder already started");recordingId=Guid.NewGuid().ToString("N");string bundle=Path.Combine(folder,"conversa-"+DateTime.Now.ToString("yyyy-MM-dd-HHmmss-fff")+"-"+recordingId.Substring(0,8));Directory.CreateDirectory(bundle);FilePath=Path.Combine(bundle,"conversa.wav");metadata=context==null?null:new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<Dictionary<string,object>>(new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(context));worker=Task.Run(()=>Run(input,output));}
 public async Task Stop(string reason="manual_stop"){stopReason=reason;Finalizing=true;cancel.Cancel();if(worker!=null)await worker.ConfigureAwait(false);}
 sealed class Samples {
  readonly Queue<float> data=new Queue<float>();double position,next;float previous;bool first=true;
  long received,padding;int sampleRate;
  public object Statistics(){return new{InputFramesReceived=Interlocked.Read(ref received),ZeroPaddingOutputSamples=Interlocked.Read(ref padding),InputSampleRate=sampleRate,Meaning="Padding can mean silence/no packet; it does not prove network loss"};}
  public void Add(float[] values,int rate){if(rate<8000||rate>384000)throw new InvalidDataException("Unexpected audio sample rate");Interlocked.Add(ref received,values.LongLength);sampleRate=rate;lock(data){foreach(float value in values){float current=float.IsNaN(value)||float.IsInfinity(value)?0:Math.Max(-1,Math.Min(1,value));if(first){previous=current;first=false;}while(next<=position){float fraction=(float)Math.Max(0,Math.Min(1,next-(position-1)));if(data.Count>=144000)throw new IOException("Recording cannot keep up with audio");data.Enqueue(previous+(current-previous)*fraction);next+=rate/48000.0;}position++;previous=current;}}}
  public float Take(){lock(data){if(data.Count>0)return data.Dequeue();Interlocked.Increment(ref padding);return 0;}}
 }
 void Publish(string phase){RecordingStatus.Write(Path.GetDirectoryName(Path.GetDirectoryName(FilePath)),phase,elapsed.Elapsed.TotalSeconds);}
 void Run(string input,string output){bool owned=false;var mic=new Samples();var remote=new Samples();Task observer=null;using(var mutex=new Mutex(false,MutexName)){
  try{try{owned=mutex.WaitOne(0);}catch(AbandonedMutexException){owned=true;}if(!owned)throw new InvalidOperationException("Another PhoneDeck recorder is active");
   Publish("starting");evidence=new RecordingEvidence(Path.GetDirectoryName(FilePath),recordingId,metadata);evidence.Event("audio_sources_selected",new{InputEndpointId=input,OutputEndpointId=output,Scope="Microphone plus all audio on the selected output"});
   if(metadata!=null&&metadata.ContainsKey("MonitorHfp")&&Convert.ToBoolean(metadata["MonitorHfp"]))observer=Task.Factory.StartNew(()=>{string last=null;while(!cancel.Token.WaitHandle.WaitOne(5000)){try{var observed=CallAudit.Observe();string current=new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(observed);if(current!=last){evidence.Event("call_state_observed",observed);last=current;}}catch(Exception e){Error="Falha no registro de estado: "+e.Message;cancel.Cancel();}}},TaskCreationOptions.LongRunning);
   using(var a=new ManualResetEventSlim())using(var b=new ManualResetEventSlim()){
    Exception captureError=null;Action<Exception> fail=e=>{Interlocked.CompareExchange(ref captureError,e,null);cancel.Cancel();};
    var one=Task.Run(()=>{try{capture(input,1,mic.Add,()=>a.Set(),cancel.Token);}catch(Exception e){fail(e);}finally{a.Set();}});
    var two=Task.Run(()=>{try{capture(output,0,remote.Add,()=>b.Set(),cancel.Token);}catch(Exception e){fail(e);}finally{b.Set();}});
    try{
     if(!a.Wait(6000)||!b.Wait(6000))throw new TimeoutException("Audio capture did not start");if(captureError!=null)throw captureError;
     evidence.Event("audio_sources_ready",new{Input=input,Output=output});
     using(var file=new FileStream(FilePath,FileMode.CreateNew,FileAccess.ReadWrite,FileShare.Read))using(var writer=new BinaryWriter(file,Encoding.ASCII)){
      WriteHeader(writer,0);long samples=0,lastHeader=0,lastCheckpoint=0;elapsed.Start();Started=true;Publish("recording");try{while(!cancel.IsCancellationRequested){long target=(long)(elapsed.Elapsed.TotalSeconds*48000);while(samples<target){int count=(int)Math.Min(960,target-samples);for(int i=0;i<count;i++){float sample=(mic.Take()+remote.Take())*.5f;writer.Write((short)Math.Round(Math.Max(-1,Math.Min(1,sample))*32767));}samples+=count;}
       if(samples*2>=uint.MaxValue-8192)throw new IOException("WAV limit reached; recording saved");if(samples-lastHeader>=48000){long end=file.Position;file.Position=0;WriteHeader(writer,(uint)(samples*2));file.Position=end;writer.Flush();file.Flush(true);lastHeader=samples;Publish("recording");}Thread.Sleep(10);
       if(samples-lastCheckpoint>=240000){evidence.Event("audio_checkpoint",new{AudioFrames=samples,DurationSeconds=elapsed.Elapsed.TotalSeconds,Input=mic.Statistics(),Output=remote.Statistics()});lastCheckpoint=samples;}
      }}finally{elapsed.Stop();long length=file.Length-44;if(length>uint.MaxValue-36)throw new IOException("WAV too large");file.Position=0;WriteHeader(writer,(uint)length);writer.Flush();}
     }
     if(captureError!=null)throw captureError;
    }finally{cancel.Cancel();Task.WaitAll(one,two);}
   }
  }catch(Exception e){Error=e.GetBaseException().Message;}finally{Finalizing=true;cancel.Cancel();elapsed.Stop();if(owned)Publish("saving");try{if(observer!=null)observer.GetAwaiter().GetResult();if(evidence!=null){evidence.Event("recording_stop_reason",new{Reason=stopReason,Error=Error});evidence.Finish(FilePath,Error,elapsed.Elapsed.TotalSeconds,new{Input=mic.Statistics(),Output=remote.Statistics()});}}catch(Exception e){Error=(Error==null?"":Error+"; ")+"Registro de integridade incompleto: "+e.GetBaseException().Message;}if(owned){Publish(String.IsNullOrEmpty(Error)?"stopped":"error");mutex.ReleaseMutex();}}
 }}
 static void WriteHeader(BinaryWriter w,uint bytes){w.Write(Encoding.ASCII.GetBytes("RIFF"));w.Write(bytes+36);w.Write(Encoding.ASCII.GetBytes("WAVEfmt "));w.Write(16);w.Write((short)1);w.Write((short)1);w.Write(48000);w.Write(96000);w.Write((short)2);w.Write((short)16);w.Write(Encoding.ASCII.GetBytes("data"));w.Write(bytes);}
}

static class RecordingCapture {
 [ComImport,Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]interface IClient {
  [PreserveSig]int Initialize(int mode,uint flags,long duration,long period,IntPtr format,IntPtr session);void GetBufferSize();void GetStreamLatency();void GetCurrentPadding();void IsFormatSupported();[PreserveSig]int GetMixFormat(out IntPtr format);void GetDevicePeriod();[PreserveSig]int Start();[PreserveSig]int Stop();void Reset();void SetEventHandle();[PreserveSig]int GetService(ref Guid iid,[MarshalAs(UnmanagedType.IUnknown)]out object result);
 }
 [ComImport,Guid("C8ADBD64-E71E-48a0-A4DE-185C395CD317"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]interface IReader {
  [PreserveSig]int GetBuffer(out IntPtr data,out uint frames,out uint flags,out ulong device,out ulong qpc);[PreserveSig]int ReleaseBuffer(uint frames);[PreserveSig]int GetNextPacketSize(out uint frames);
 }
 static void Check(int hr){Marshal.ThrowExceptionForHR(hr);}static void Free(object o){if(o!=null&&Marshal.IsComObject(o))Marshal.ReleaseComObject(o);}
 public static void Run(string id,int flow,Action<float[],int> sink,Action ready,CancellationToken stop){IDevices ds=null;IDevice device=null;object clientObject=null,readerObject=null;IntPtr format=IntPtr.Zero;IClient client=null;bool started=false;
  try{ds=(IDevices)new Enumerator();Check(ds.GetDevice(id,out device));Guid iid=typeof(IClient).GUID;Check(device.Activate(ref iid,23,IntPtr.Zero,out clientObject));client=(IClient)clientObject;Check(client.GetMixFormat(out format));
   int tag=(ushort)Marshal.ReadInt16(format),channels=(ushort)Marshal.ReadInt16(format,2),rate=Marshal.ReadInt32(format,4),align=(ushort)Marshal.ReadInt16(format,12),bits=(ushort)Marshal.ReadInt16(format,14);if(tag==65534)tag=Marshal.ReadInt32(format,24);
   if(channels<1||channels>32||align!=channels*(bits/8)||!(tag==3&&bits==32||tag==1&&(bits==16||bits==24||bits==32)))throw new InvalidOperationException("Unsupported recording format");
   Check(client.Initialize(0,flow==0?0x20000U:0U,1000000,0,format,IntPtr.Zero));iid=typeof(IReader).GUID;Check(client.GetService(ref iid,out readerObject));var reader=(IReader)readerObject;Check(client.Start());started=true;ready();
   while(!stop.IsCancellationRequested){uint available;Check(reader.GetNextPacketSize(out available));int packets=0;while(available>0&&!stop.IsCancellationRequested&&packets++<100){IntPtr data;uint frames,flags;ulong devicePosition,qpc;Check(reader.GetBuffer(out data,out frames,out flags,out devicePosition,out qpc));try{
    long size=(long)frames*align;if(size>2097152)throw new InvalidDataException("Audio packet too large");var mono=new float[frames];if((flags&2)==0&&data!=IntPtr.Zero){var bytes=new byte[(int)size];Marshal.Copy(data,bytes,0,bytes.Length);for(int f=0;f<frames;f++){float sum=0;for(int c=0;c<channels;c++){int at=f*align+c*(bits/8);float value;if(tag==3)value=BitConverter.ToSingle(bytes,at);else if(bits==16)value=BitConverter.ToInt16(bytes,at)/32768f;else if(bits==32)value=BitConverter.ToInt32(bytes,at)/2147483648f;else{int n=bytes[at]|bytes[at+1]<<8|bytes[at+2]<<16;if((n&0x800000)!=0)n|=unchecked((int)0xff000000);value=n/8388608f;}sum+=value;}mono[f]=sum/channels;}}sink(mono,rate);
   }finally{Check(reader.ReleaseBuffer(frames));}Check(reader.GetNextPacketSize(out available));}Thread.Sleep(10);}
  }finally{if(started)client.Stop();if(format!=IntPtr.Zero)Marshal.FreeCoTaskMem(format);Free(readerObject);Free(clientObject);Free(device);Free(ds);}
 }
}
