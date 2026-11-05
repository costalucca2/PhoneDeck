using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
[assembly:System.Reflection.AssemblyFileVersion("0.1.0.2")]
// Read-only KS property discovery. Never creates a streaming pin or sends SET.
static class AudioPinProbe {
 [StructLayout(LayoutKind.Sequential)]struct Interface {public int Size;public Guid Class;public uint Flags;public UIntPtr Reserved;}
 [DllImport("setupapi.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern IntPtr SetupDiGetClassDevs(ref Guid cls,string enumerator,IntPtr parent,uint flags);
 [DllImport("setupapi.dll",SetLastError=true)]static extern bool SetupDiEnumDeviceInterfaces(IntPtr list,IntPtr device,ref Guid cls,uint index,ref Interface data);
 [DllImport("setupapi.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr list,ref Interface data,IntPtr detail,uint size,out uint required,IntPtr device);
 [DllImport("setupapi.dll")]static extern bool SetupDiDestroyDeviceInfoList(IntPtr list);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern SafeFileHandle CreateFile(string path,uint access,uint share,IntPtr security,uint disposition,uint flags,IntPtr template);
 [DllImport("kernel32.dll",SetLastError=true)]static extern bool DeviceIoControl(SafeFileHandle file,uint code,byte[] input,int inputSize,byte[] output,int outputSize,out int returned,IntPtr overlapped);
 static byte[] Query(SafeFileHandle file,uint property,uint? pin){
  byte[] request=new byte[pin.HasValue?32:24];new Guid("8c134960-51ad-11cf-878a-94f801c10000").ToByteArray().CopyTo(request,0);
  BitConverter.GetBytes(property).CopyTo(request,16);BitConverter.GetBytes(1U).CopyTo(request,20);if(pin.HasValue)BitConverter.GetBytes(pin.Value).CopyTo(request,24);
  byte[] output=new byte[16384];int returned;if(!DeviceIoControl(file,0x002F0003,request,request.Length,output,output.Length,out returned,IntPtr.Zero))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
  Array.Resize(ref output,returned);return output;
 }
 static int Main(){var log=new List<string>();int found=0;Guid cls=new Guid("6994ad04-93ef-11d0-a3cc-00a0c9223196");IntPtr list=SetupDiGetClassDevs(ref cls,null,IntPtr.Zero,18);
  try{if(list==new IntPtr(-1))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());for(uint i=0;;i++){
   var data=new Interface{Size=Marshal.SizeOf(typeof(Interface))};if(!SetupDiEnumDeviceInterfaces(list,IntPtr.Zero,ref cls,i,ref data)){int error=Marshal.GetLastWin32Error();if(error==259)break;throw new System.ComponentModel.Win32Exception(error);}
   uint size;SetupDiGetDeviceInterfaceDetail(list,ref data,IntPtr.Zero,0,out size,IntPtr.Zero);if(size<8||size>65536)continue;IntPtr memory=Marshal.AllocHGlobal((int)size);
   try{Marshal.WriteInt32(memory,IntPtr.Size==8?8:6);if(!SetupDiGetDeviceInterfaceDetail(list,ref data,memory,size,out size,IntPtr.Zero))continue;string path=Marshal.PtrToStringUni(IntPtr.Add(memory,4));if(path==null||path.IndexOf("bthhf",StringComparison.OrdinalIgnoreCase)<0)continue;
    found++;log.Add("HfpAudioInterface="+found);using(var file=CreateFile(path,0x80000000,3,IntPtr.Zero,3,0,IntPtr.Zero)){
     if(file.IsInvalid){log.Add("OpenError="+Marshal.GetLastWin32Error());continue;}
     try{byte[] countData=Query(file,1,null);if(countData.Length<4)throw new IOException("Missing pin count");uint count=BitConverter.ToUInt32(countData,0);log.Add("Pins="+count);if(count>64)throw new IOException("Invalid pin count");for(uint pin=0;pin<count;pin++){try{byte[] flow=Query(file,2,pin);byte[] ranges=Query(file,3,pin);log.Add("Pin="+pin+";Interfaces="+BitConverter.ToString(Query(file,5,pin))+";Mediums="+BitConverter.ToString(Query(file,6,pin))+";Communication="+BitConverter.ToUInt32(Query(file,7,pin),0));log.Add("Pin="+pin+";DataFlow="+(flow.Length>=4?BitConverter.ToUInt32(flow,0).ToString():"unknown")+";Ranges="+BitConverter.ToString(ranges));}catch(Exception ex){log.Add("PinReadError="+ex.Message);}}}catch(Exception ex){log.Add("PropertyReadError="+ex.Message);}
    }
   }finally{Marshal.FreeHGlobal(memory);}
  }log.Add("HfpAudioInterfaces="+found+";ReadOnly=True;NoStreamOrCallCommands=True");return 0;
  }catch(Exception ex){log.Add("Error="+ex.Message);return 1;}finally{if(list!=new IntPtr(-1))SetupDiDestroyDeviceInfoList(list);File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"audio-pin-probe.local.txt"),log);}
 }
}
