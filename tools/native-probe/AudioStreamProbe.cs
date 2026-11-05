using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Windows.ApplicationModel.Calls;
#if !NATIVE_AUDIO_PACKAGE
[assembly:System.Reflection.AssemblyFileVersion("0.1.0.5")]
#endif
// Opens PCM pins and verifies state transitions without transferring samples or calls.
static class AudioStreamProbe {
 [StructLayout(LayoutKind.Sequential)]struct Interface {public int Size;public Guid Class;public uint Flags;public UIntPtr Reserved;}
 [DllImport("setupapi.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern IntPtr SetupDiGetClassDevs(ref Guid cls,string enumerator,IntPtr parent,uint flags);
 [DllImport("setupapi.dll",SetLastError=true)]static extern bool SetupDiEnumDeviceInterfaces(IntPtr list,IntPtr device,ref Guid cls,uint index,ref Interface data);
 [DllImport("setupapi.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr list,ref Interface data,IntPtr detail,uint size,out uint required,IntPtr device);
 [DllImport("setupapi.dll")]static extern bool SetupDiDestroyDeviceInfoList(IntPtr list);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern SafeFileHandle CreateFile(string path,uint access,uint share,IntPtr security,uint disposition,uint flags,IntPtr template);
 [DllImport("ksuser.dll")]static extern uint KsCreatePin(SafeFileHandle filter,IntPtr connect,uint access,out SafeFileHandle pin);
 [DllImport("kernel32.dll",SetLastError=true)]static extern bool DeviceIoControl(SafeFileHandle file,uint code,IntPtr input,int inputSize,IntPtr output,int outputSize,out uint returned,IntPtr overlapped);
 [DllImport("kernel32.dll",SetLastError=true)]static extern bool GetOverlappedResult(SafeFileHandle file,IntPtr overlapped,out uint returned,bool wait);
 [DllImport("kernel32.dll",SetLastError=true)]static extern bool CancelIoEx(SafeFileHandle file,IntPtr overlapped);
 static void State(SafeFileHandle pin,uint state){
  byte[] property=new byte[24];new Guid("1d58c920-ac9b-11cf-a5d6-28db04c10000").ToByteArray().CopyTo(property,0);BitConverter.GetBytes(2U).CopyTo(property,20);
  IntPtr request=Marshal.AllocHGlobal(24),value=Marshal.AllocHGlobal(4),overlap=Marshal.AllocHGlobal(Marshal.SizeOf(typeof(System.Threading.NativeOverlapped)));
  using(var signal=new System.Threading.ManualResetEvent(false)){try{Marshal.Copy(property,0,request,24);Marshal.WriteInt32(value,(int)state);Marshal.StructureToPtr(new System.Threading.NativeOverlapped{EventHandle=signal.SafeWaitHandle.DangerousGetHandle()},overlap,false);uint transferred;
   if(!DeviceIoControl(pin,0x002F0003,request,24,value,4,out transferred,overlap)){int error=Marshal.GetLastWin32Error();if(error!=997)throw new System.ComponentModel.Win32Exception(error);if(!signal.WaitOne(3000)){CancelIoEx(pin,overlap);GetOverlappedResult(pin,overlap,out transferred,true);throw new TimeoutException("KS state transition timed out");}if(!GetOverlappedResult(pin,overlap,out transferred,false))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());}
  }finally{Marshal.FreeHGlobal(request);Marshal.FreeHGlobal(value);Marshal.FreeHGlobal(overlap);}}
 }
 static void UInt(byte[] buffer,int offset,uint value){BitConverter.GetBytes(value).CopyTo(buffer,offset);}
 static void Short(byte[] buffer,int offset,ushort value){BitConverter.GetBytes(value).CopyTo(buffer,offset);}
 static byte[] Format(uint pin){
  var data=new byte[160];new Guid("1a8766a0-62ce-11cf-a5d6-28db04c10000").ToByteArray().CopyTo(data,0);
  new Guid("4747b320-62ce-11cf-a5d6-28db04c10000").ToByteArray().CopyTo(data,24);
  UInt(data,48,pin);UInt(data,64,0x40000000);UInt(data,68,1);
  UInt(data,72,88);UInt(data,80,2);
  new Guid("73647561-0000-0010-8000-00aa00389b71").ToByteArray().CopyTo(data,88);
  new Guid("00000001-0000-0010-8000-00aa00389b71").ToByteArray().CopyTo(data,104);
  new Guid("05589f81-c356-11ce-bf01-00aa0055595a").ToByteArray().CopyTo(data,120);
  Short(data,136,1);Short(data,138,1);UInt(data,140,8000);UInt(data,144,16000);Short(data,148,2);Short(data,150,16);return data;
 }
 static int Main(){return Probe();}
 public static int Probe(){var log=new List<string>();Guid cls=new Guid("6994ad04-93ef-11d0-a3cc-00a0c9223196");IntPtr list=IntPtr.Zero;try{
  if(PhoneCallManager.IsCallActive||PhoneCallManager.IsCallIncoming){log.Add("Aborted=CallActiveOrIncoming;NoAudioHandleOpened");return 2;}
  list=SetupDiGetClassDevs(ref cls,null,IntPtr.Zero,18);if(list==new IntPtr(-1))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());int found=0;
  for(uint i=0;;i++){var data=new Interface{Size=Marshal.SizeOf(typeof(Interface))};if(!SetupDiEnumDeviceInterfaces(list,IntPtr.Zero,ref cls,i,ref data)){if(Marshal.GetLastWin32Error()==259)break;throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());}
   uint size;SetupDiGetDeviceInterfaceDetail(list,ref data,IntPtr.Zero,0,out size,IntPtr.Zero);if(size<8||size>65536)continue;IntPtr detail=Marshal.AllocHGlobal((int)size);
   try{Marshal.WriteInt32(detail,8);if(!SetupDiGetDeviceInterfaceDetail(list,ref data,detail,size,out size,IntPtr.Zero))continue;string path=Marshal.PtrToStringUni(IntPtr.Add(detail,4));if(path==null||path.IndexOf("bthhf",StringComparison.OrdinalIgnoreCase)<0)continue;found++;
    if(PhoneCallManager.IsCallActive||PhoneCallManager.IsCallIncoming)throw new Exception("Call detected; audio probe stopped");
    using(var filter=CreateFile(path,0xC0000000,3,IntPtr.Zero,3,0x40000000,IntPtr.Zero)){
     if(filter.IsInvalid){log.Add("FilterOpenError="+Marshal.GetLastWin32Error());continue;}
     int running=0;var opened=new List<Tuple<uint,SafeFileHandle>>();
     try{
      foreach(uint index in new uint[]{0,3}){byte[] bytes=Format(index);IntPtr format=Marshal.AllocHGlobal(bytes.Length);try{Marshal.Copy(bytes,0,format,bytes.Length);SafeFileHandle pin;uint error=KsCreatePin(filter,format,0xC0000000,out pin);log.Add("PcmPin="+index+";OpenResult="+error);if(error!=0){if(pin!=null)pin.Dispose();throw new System.ComponentModel.Win32Exception((int)error);}opened.Add(Tuple.Create(index,pin));foreach(uint state in new uint[]{1,2}){if(PhoneCallManager.IsCallActive||PhoneCallManager.IsCallIncoming)throw new Exception("Call detected: audio test aborted");State(pin,state);log.Add("Pin="+index+";State="+state+";Result=OK");}}finally{Marshal.FreeHGlobal(format);}}
      foreach(var pair in opened){try{if(PhoneCallManager.IsCallActive||PhoneCallManager.IsCallIncoming)throw new Exception("Call detected: audio test aborted");State(pair.Item2,3);running++;log.Add("Pin="+pair.Item1+";State=3;Result=OK");}catch(Exception ex){log.Add("Pin="+pair.Item1+";DuplexStateError="+ex.Message);}}
     }finally{foreach(var pair in opened){try{State(pair.Item2,0);}catch{}pair.Item2.Dispose();}log.Add("AllPcmPinsClosed=True");}log.Add("VoiceRunReady="+(running==2));if(running!=2)return 3;
    }
   }finally{Marshal.FreeHGlobal(detail);}
  }log.Add("HfpAudioInterfaces="+found+";StateProbe=True;NoSamples=True;NoCallCommands=True");return found==1?0:4;
 }catch(Exception ex){log.Add("Error="+ex.Message);return 1;}finally{if(list!=IntPtr.Zero&&list!=new IntPtr(-1))SetupDiDestroyDeviceInfoList(list);File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"audio-stream-probe.local.txt"),log);}}
}
