using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using Windows.ApplicationModel.Calls;
using Windows.Devices.Enumeration;
[assembly:System.Reflection.AssemblyFileVersion("0.1.0.43")]
// Defaults to getters. Explicit --audio-setup requests access, connects and selects audio only; no call commands.
static class NativeHfpInteropProbe {
 [DllImport("combase.dll")]static extern int RoInitialize(uint mode);
 [DllImport("combase.dll")]static extern void RoUninitialize();
 [DllImport("combase.dll",CharSet=CharSet.Unicode)]static extern int WindowsCreateString(string text,int length,out IntPtr value);
 [DllImport("combase.dll")]static extern int WindowsDeleteString(IntPtr value);
 [DllImport("combase.dll")]static extern int RoGetActivationFactory(IntPtr name,ref Guid iid,out IntPtr factory);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int FromId(IntPtr self,IntPtr id,out IntPtr value);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int GetObject(IntPtr self,out IntPtr value);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int GetSize(IntPtr self,out uint value);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int NoArg(IntPtr self);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int GetByte(IntPtr self,out byte value);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int GetInt(IntPtr self,out int value);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int SetAudio(IntPtr self,byte transfer,out IntPtr operation);
 static void Await(IntPtr operation){IntPtr info=IntPtr.Zero;try{Guid iid=typeof(Windows.Foundation.IAsyncInfo).GUID;Check(Marshal.QueryInterface(operation,ref iid,out info),"AsyncInfo");var until=DateTime.UtcNow.AddSeconds(8);while(true){int status;Check(Method<GetInt>(info,7)(info,out status),"AsyncStatus");if(status==1)return;if(status!=0)throw new Exception("Async did not complete: "+status);if(DateTime.UtcNow>=until){Method<NoArg>(info,9)(info);throw new TimeoutException("Audio setup timed out");}Thread.Sleep(100);}}finally{if(info!=IntPtr.Zero)Marshal.Release(info);}}
 static int AsyncInt(IntPtr operation){try{Await(operation);int result;Check(Method<GetInt>(operation,8)(operation,out result),"EnumResult");return result;}finally{Marshal.Release(operation);}}
 static bool AsyncBool(IntPtr operation){try{Await(operation);byte result;Check(Method<GetByte>(operation,8)(operation,out result),"BoolResult");return result!=0;}finally{Marshal.Release(operation);}}
 static void EnsureIdle(){if(PhoneCallManager.IsCallActive||PhoneCallManager.IsCallIncoming)throw new Exception("Active or incoming call: setup aborted");}
 static readonly StringBuilder log=new StringBuilder();
 static void Record(string text){log.AppendLine(text);string root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"PhoneDeck");Directory.CreateDirectory(root);File.WriteAllText(Path.Combine(root,"native-hfp-interop.local.txt"),log.ToString());}
 static T Method<T>(IntPtr value,int slot)where T:class {if(value==IntPtr.Zero)throw new ArgumentException("Missing interface");IntPtr table=Marshal.ReadIntPtr(value);return (T)(object)Marshal.GetDelegateForFunctionPointer(Marshal.ReadIntPtr(table,slot*IntPtr.Size),typeof(T));}
 static void Check(int result,string operation){Record(operation+"=0x"+result.ToString("X8"));Marshal.ThrowExceptionForHR(result);}
 static T Get<T>(Windows.Foundation.IAsyncOperation<T> operation){var until=DateTime.UtcNow.AddSeconds(10);while(operation.Status==Windows.Foundation.AsyncStatus.Started&&DateTime.UtcNow<until)Thread.Sleep(20);if(operation.Status==Windows.Foundation.AsyncStatus.Started){operation.Cancel();throw new TimeoutException("Enumeration timed out");}return operation.GetResults();}
 [MTAThread]static int Main(string[] args){IntPtr className=IntPtr.Zero,id=IntPtr.Zero,factory=IntPtr.Zero,activation=IntPtr.Zero,statics=IntPtr.Zero,transport=IntPtr.Zero,device=IntPtr.Zero,calls=IntPtr.Zero;bool initialized=false;try{
  Record("AudioSetupRequested="+(Array.IndexOf(args,"--audio-setup")>=0));int init=RoInitialize(1);Check(init,"RoInitialize");initialized=true;
  var devices=Get(DeviceInformation.FindAllAsync(PhoneLineTransportDevice.GetDeviceSelector()));Record("Transports="+devices.Count);if(devices.Count!=1)throw new Exception("Exactly one paired phone required");
  const string name="Microsoft.Bluetooth.Profiles.Hfp.Internal.HfpTransportDevice";Check(WindowsCreateString(name,name.Length,out className),"ClassString");
  Guid activationIid=new Guid("00000035-0000-0000-c000-000000000046");Check(RoGetActivationFactory(className,ref activationIid,out activation),"BaseActivationFactory");Check(Method<GetObject>(activation,6)(activation,out statics),"CreateStaticsObject");Guid iid=new Guid("6410e692-41b6-458c-9a0c-894a373ff3a3");Check(Marshal.QueryInterface(statics,ref iid,out factory),"TransportFactoryQuery");
  Check(WindowsCreateString(devices[0].Id,devices[0].Id.Length,out id),"DeviceString");
  Check(Method<FromId>(factory,6)(factory,id,out transport),"FromId");
  byte registered;Check(Method<GetByte>(transport,11)(transport,out registered),"IsRegistered");Record("Registered="+(registered!=0));
  if(Array.IndexOf(args,"--audio-setup")>=0){EnsureIdle();IntPtr access;Check(Method<GetObject>(transport,6)(transport,out access),"RequestAccessAsync");int grant=AsyncInt(access);Record("AccessResult="+grant);if(grant!=(int)DeviceAccessStatus.Allowed)throw new Exception("Native transport access denied");if(registered==0)Check(Method<NoArg>(transport,7)(transport),"Register");EnsureIdle();IntPtr connect;Check(Method<GetObject>(transport,13)(transport,out connect),"ConnectAsync");bool connected=AsyncBool(connect);Record("Connected="+connected);if(!connected)throw new Exception("Native transport did not connect");}
  Check(Method<GetObject>(transport,16)(transport,out device),"GetHandsFreeDevice");Record("HandsFreeObject="+(device!=IntPtr.Zero));
  Check(Method<GetObject>(device,12)(device,out calls),"GetCallList");uint count;Check(Method<GetSize>(calls,7)(calls,out count),"GetCallCount");Record("CallCount="+count);
  if(Array.IndexOf(args,"--audio-setup")>=0){EnsureIdle();if(count!=0)throw new Exception("Phone has a call; audio test skipped");IntPtr operation;Check(Method<SetAudio>(device,13)(device,1,out operation),"SetAudioTransferAsync");Record("AudioTransferResult="+AsyncInt(operation));Record("NoCallCommands=True;NoMicrophoneCapture=True");}else Record("ReadOnly=True;NoCallOrAudioCommands=True");return 0;
 }catch(Exception ex){Record("Error="+ex.HResult.ToString("X8")+";"+ex.Message);return 1;}finally{
  foreach(IntPtr pointer in new[]{calls,device,transport,factory,statics,activation})if(pointer!=IntPtr.Zero)Marshal.Release(pointer);
  if(id!=IntPtr.Zero)WindowsDeleteString(id);if(className!=IntPtr.Zero)WindowsDeleteString(className);if(initialized)RoUninitialize();
 }}
}
