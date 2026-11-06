using System;
using System.Runtime.InteropServices;
using System.Threading;
using PhoneDeckAudio;

// Shared-mode capture for a user-requested level check. No audio is saved or played.
static class MicrophoneCheck {
 [ComImport,Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]interface IClient {
  [PreserveSig]int Initialize(int mode,uint flags,long duration,long period,IntPtr format,IntPtr session);
  void GetBufferSize();void GetStreamLatency();void GetCurrentPadding();void IsFormatSupported();
  [PreserveSig]int GetMixFormat(out IntPtr format);
  void GetDevicePeriod();[PreserveSig]int Start();[PreserveSig]int Stop();void Reset();void SetEventHandle();
  [PreserveSig]int GetService(ref Guid iid,[MarshalAs(UnmanagedType.IUnknown)]out object service);
 }
 [ComImport,Guid("C8ADBD64-E71E-48a0-A4DE-185C395CD317"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]interface ICapture {
  [PreserveSig]int GetBuffer(out IntPtr data,out uint frames,out uint flags,out ulong position,out ulong timestamp);
  [PreserveSig]int ReleaseBuffer(uint frames);[PreserveSig]int GetNextPacketSize(out uint frames);
 }
 static void Check(int hr){Marshal.ThrowExceptionForHR(hr);}
 static void Free(object value){if(value!=null&&Marshal.IsComObject(value))Marshal.ReleaseComObject(value);}
 public static float Run(Action<float> report,CancellationToken cancel){
  IDevices ds=null;IDevice device=null;object clientObject=null,captureObject=null;IntPtr format=IntPtr.Zero;IClient client=null;bool started=false;float peak=0;
  try{ds=(IDevices)new Enumerator();Check(ds.GetDefaultAudioEndpoint(1,2,out device));Guid iid=typeof(IClient).GUID;Check(device.Activate(ref iid,23,IntPtr.Zero,out clientObject));client=(IClient)clientObject;Check(client.GetMixFormat(out format));
   int tag=(ushort)Marshal.ReadInt16(format),channels=(ushort)Marshal.ReadInt16(format,2),align=(ushort)Marshal.ReadInt16(format,12),bits=(ushort)Marshal.ReadInt16(format,14);
   if(tag==65534)tag=Marshal.ReadInt32(format,24);if(channels<1||channels>32||align<1||!(tag==3&&bits==32||tag==1&&(bits==16||bits==24||bits==32)))throw new InvalidOperationException("Formato do microfone não suportado no teste de nível.");
   Check(client.Initialize(0,0,1000000,0,format,IntPtr.Zero));iid=typeof(ICapture).GUID;Check(client.GetService(ref iid,out captureObject));var capture=(ICapture)captureObject;Check(client.Start());started=true;
   var until=DateTime.UtcNow.AddSeconds(6);while(DateTime.UtcNow<until&&!cancel.IsCancellationRequested){uint available;Check(capture.GetNextPacketSize(out available));float current=0;
    while(available>0){IntPtr data;uint frames,flags;ulong position,timestamp;Check(capture.GetBuffer(out data,out frames,out flags,out position,out timestamp));try{
     long size=(long)frames*align;if(size>2097152)throw new InvalidOperationException("Pacote de áudio inesperado.");
     if((flags&2)==0&&data!=IntPtr.Zero){var bytes=new byte[(int)size];Marshal.Copy(data,bytes,0,bytes.Length);int width=bits/8;for(int offset=0;offset+width<=bytes.Length;offset+=width){float value;if(tag==3)value=BitConverter.ToSingle(bytes,offset);else if(bits==16)value=BitConverter.ToInt16(bytes,offset)/32768f;else if(bits==32)value=BitConverter.ToInt32(bytes,offset)/2147483648f;else{int n=(bytes[offset]|bytes[offset+1]<<8|bytes[offset+2]<<16);if((n&0x800000)!=0)n|=unchecked((int)0xff000000);value=n/8388608f;}if(!float.IsNaN(value))current=Math.Max(current,Math.Min(1,Math.Abs(value)));}}
    }finally{Check(capture.ReleaseBuffer(frames));}Check(capture.GetNextPacketSize(out available));}
    peak=Math.Max(peak,current);report(current);Thread.Sleep(40);
   }return peak;
  }finally{if(started)client.Stop();if(format!=IntPtr.Zero)Marshal.FreeCoTaskMem(format);Free(captureObject);Free(clientObject);Free(device);Free(ds);}
 }
}
