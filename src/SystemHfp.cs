using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.Calls;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth;

namespace PhoneDeckNative {
 public interface ISystemHfp : IDisposable {
  void Connect(DateTime deadline);
  uint CallCount();
  bool Incoming {get;}
  bool Active {get;}
  void Command(string action,string number,DateTime deadline);
  void LocalAudio(DateTime deadline);
 }
 // Testable policy; call mutations are never retried, including after an audio error.
 public sealed class SystemHfpDispatch {
  readonly ISystemHfp phone;
  public SystemHfpDispatch(ISystemHfp phone){this.phone=phone;}
  static void Deadline(DateTime limit){if(DateTime.UtcNow>=limit)throw new TimeoutException("Solicitação expirada; nenhum novo comando enviado.");}
  public Dictionary<string,object> Execute(string action,string number,DateTime limit){
   Deadline(limit);
   if(action!="status"&&action!="dial"&&action!="answer"&&action!="hangup"&&action!="audio")throw new ArgumentException("Comando inválido.");
   if(action=="dial"&&!Regex.IsMatch(number??"",@"\A(?:\+[1-9][0-9]{7,14}|[0-9]{8,14})\z"))throw new ArgumentException("Número inválido.");
   phone.Connect(limit);Deadline(limit);
   uint count=phone.CallCount();bool incoming=phone.Incoming,active=phone.Active;
   if(action=="dial"&&(count!=0||incoming||active))throw new InvalidOperationException("Celular ocupado; discagem bloqueada.");
   if(action=="answer"&&(count!=1||!incoming||active))throw new InvalidOperationException("Nenhuma chamada recebida única confirmada.");
   if(action=="hangup"&&count!=1)throw new InvalidOperationException("Desligar exige uma única chamada confirmada.");
   if(action=="audio"&&(count!=0||incoming||active))throw new InvalidOperationException("Preparação exige celular ocioso.");
   bool sent=false,audio=false;string warning="";
   if(action=="dial"||action=="answer"||action=="hangup"){Deadline(limit);phone.Command(action,number,limit);sent=true;}
   if(action=="audio"||action=="dial"||action=="answer"){
    try{Deadline(limit);phone.LocalAudio(limit);audio=true;}catch(Exception e){if(!sent)throw;warning=" Comando de chamada confirmado, mas preparação do áudio falhou: "+e.Message+" Não repita a ligação.";}
   }
   return new Dictionary<string,object>{{"Ok",true},{"Backend","HFP nativo Windows"},{"Device","Celular Bluetooth"},{"Connected",true},{"Call",active?1:0},{"Setup",incoming?1:0},{"Held",0},{"CallCount",count},{"AudioPrepared",audio},{"AudioValidated",false},{"Message",(sent?"Comando confirmado pelo transporte nativo.":audio?"Rota de voz local aceita pelo Windows; conversa ainda não validada.":"Controle nativo conectado; conversa ainda não validada.")+warning}};
  }
 }
 // Windows 10 internal ABI. Resolve the exact interfaces or fail closed; no UI/URI fallback.
 public sealed class SystemHfp : ISystemHfp {
  IntPtr transport,device;bool initialized;BluetoothDevice link;
  public bool IsConnected {get{return device!=IntPtr.Zero;}}
  public bool AudioPrepared {get;private set;}
  [DllImport("combase.dll")]static extern int RoInitialize(uint mode);
  [DllImport("combase.dll")]static extern void RoUninitialize();
  [DllImport("combase.dll",CharSet=CharSet.Unicode)]static extern int WindowsCreateString(string text,int length,out IntPtr value);
  [DllImport("combase.dll")]static extern int WindowsDeleteString(IntPtr value);
  [DllImport("combase.dll")]static extern int RoGetActivationFactory(IntPtr name,ref Guid iid,out IntPtr factory);
  [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int ObjectMethod(IntPtr self,out IntPtr value);
  [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int StringMethod(IntPtr self,IntPtr value,out IntPtr result);
  [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int ByteMethod(IntPtr self,out byte value);
  [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int IntMethod(IntPtr self,out int value);
  [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int SizeMethod(IntPtr self,out uint value);
  [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int AddressMethod(IntPtr self,out ulong value);
  [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int VoidMethod(IntPtr self);
  [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int AudioMethod(IntPtr self,byte value,out IntPtr result);
  static T Method<T>(IntPtr obj,int slot)where T:class {if(obj==IntPtr.Zero)throw new InvalidOperationException("Interface HFP ausente.");return (T)(object)Marshal.GetDelegateForFunctionPointer(Marshal.ReadIntPtr(Marshal.ReadIntPtr(obj),slot*IntPtr.Size),typeof(T));}
  static void Check(int hr){Marshal.ThrowExceptionForHR(hr);}
  static void Limit(DateTime limit){if(DateTime.UtcNow>=limit)throw new TimeoutException("Prazo da operação Bluetooth expirou.");}
  static void Release(IntPtr p){if(p!=IntPtr.Zero)Marshal.Release(p);}
  static IntPtr Text(string value){IntPtr result;Check(WindowsCreateString(value,value.Length,out result));return result;}
  static void Wait(IntPtr op,DateTime limit){IntPtr info=IntPtr.Zero;try{Guid iid=typeof(Windows.Foundation.IAsyncInfo).GUID;Check(Marshal.QueryInterface(op,ref iid,out info));while(true){int status;Check(Method<IntMethod>(info,7)(info,out status));if(status==1)return;if(status!=0){int error;Check(Method<IntMethod>(info,8)(info,out error));if(error<0)Check(error);throw new InvalidOperationException("Operação HFP não concluída.");}if(DateTime.UtcNow>=limit){Method<VoidMethod>(info,9)(info);throw new TimeoutException("Sem confirmação HFP; comando não será repetido.");}Thread.Sleep(25);}}finally{Release(info);}}
  static int Result(IntPtr op,DateTime limit){try{Wait(op,limit);int result;Check(Method<IntMethod>(op,8)(op,out result));return result;}finally{Release(op);}}
  static bool BoolResult(IntPtr op,DateTime limit){try{Wait(op,limit);byte result;Check(Method<ByteMethod>(op,8)(op,out result));return result!=0;}finally{Release(op);}}
  static void Idle(){if(PhoneCallManager.IsCallActive||PhoneCallManager.IsCallIncoming)throw new InvalidOperationException("Conecte o PhoneDeck com o celular ocioso; chamada preservada.");}
  public bool Incoming {get{return PhoneCallManager.IsCallIncoming;}}
  public bool Active {get{return PhoneCallManager.IsCallActive;}}
  public void Connect(DateTime limit){
   if(device!=IntPtr.Zero){if(link!=null&&link.ConnectionStatus==BluetoothConnectionStatus.Connected)return;Idle();Dispose();}
   Idle();Limit(limit);
   Check(RoInitialize(1));initialized=true;
   IntPtr name=IntPtr.Zero,id=IntPtr.Zero,activation=IntPtr.Zero,instance=IntPtr.Zero,factory=IntPtr.Zero;
   try{
    var list=DeviceInformation.FindAllAsync(PhoneLineTransportDevice.GetDeviceSelector());
    try{while(list.Status==Windows.Foundation.AsyncStatus.Started){Limit(limit);Thread.Sleep(25);}var devices=list.GetResults();if(devices.Count!=1)throw new InvalidOperationException("É necessário exatamente um celular pareado.");id=Text(devices[0].Id);}finally{if(list.Status==Windows.Foundation.AsyncStatus.Started)list.Cancel();list.Close();}
    name=Text("Microsoft.Bluetooth.Profiles.Hfp.Internal.HfpTransportDevice");Guid iid=new Guid("00000035-0000-0000-c000-000000000046");Check(RoGetActivationFactory(name,ref iid,out activation));Check(Method<ObjectMethod>(activation,6)(activation,out instance));iid=new Guid("6410e692-41b6-458c-9a0c-894a373ff3a3");Check(Marshal.QueryInterface(instance,ref iid,out factory));Check(Method<StringMethod>(factory,6)(factory,id,out transport));
    IntPtr op;Limit(limit);Check(Method<ObjectMethod>(transport,6)(transport,out op));if(Result(op,limit)!=(int)DeviceAccessStatus.Allowed)throw new InvalidOperationException("Permissão Bluetooth negada. Instale a identidade nativa própria do PhoneDeck.");
    byte registered;Check(Method<ByteMethod>(transport,11)(transport,out registered));if(registered==0){Idle();Limit(limit);Check(Method<VoidMethod>(transport,7)(transport));}
    Idle();Limit(limit);Check(Method<ObjectMethod>(transport,13)(transport,out op));if(!BoolResult(op,limit))throw new InvalidOperationException("Celular não conectou ao transporte de voz.");
    Check(Method<ObjectMethod>(transport,16)(transport,out device));Guid handsFree=new Guid("c7975e09-f9fc-4361-b2d8-d495fc2da743");IntPtr verified;Check(Marshal.QueryInterface(device,ref handsFree,out verified));Release(verified);
    ulong address;Check(Method<AddressMethod>(device,6)(device,out address));var lookup=BluetoothDevice.FromBluetoothAddressAsync(address);
    try{while(lookup.Status==Windows.Foundation.AsyncStatus.Started){Limit(limit);Thread.Sleep(25);}link=lookup.GetResults();}finally{if(lookup.Status==Windows.Foundation.AsyncStatus.Started)lookup.Cancel();lookup.Close();}
    if(link==null||link.ConnectionStatus!=BluetoothConnectionStatus.Connected)throw new InvalidOperationException("Vínculo Bluetooth desconectado; voz não preparada.");
   }catch{Dispose();throw;}finally{Release(factory);Release(instance);Release(activation);if(id!=IntPtr.Zero)WindowsDeleteString(id);if(name!=IntPtr.Zero)WindowsDeleteString(name);}
  }
  public uint CallCount(){IntPtr calls;Check(Method<ObjectMethod>(device,12)(device,out calls));try{uint count;Check(Method<SizeMethod>(calls,7)(calls,out count));return count;}finally{Release(calls);}}
  public void LocalAudio(DateTime limit){AudioPrepared=false;Limit(limit);IntPtr op;Check(Method<AudioMethod>(device,13)(device,1,out op));if(Result(op,limit)!=0)throw new InvalidOperationException("Windows recusou a transferência de áudio ao PC.");AudioPrepared=true;}
  public void Command(string action,string number,DateTime limit){Limit(limit);IntPtr op;if(action=="dial"){IntPtr text=Text(number);try{Check(Method<StringMethod>(device,15)(device,text,out op));}finally{WindowsDeleteString(text);}}else if(action=="answer")Check(Method<ObjectMethod>(device,17)(device,out op));else if(action=="hangup")Check(Method<ObjectMethod>(device,19)(device,out op));else throw new ArgumentException("Comando de chamada inválido.");if(Result(op,limit)!=0)throw new InvalidOperationException("Telefone recusou o comando; nenhuma repetição será feita.");}
  public void Dispose(){AudioPrepared=false;if(link!=null){link.Dispose();link=null;}Release(device);device=IntPtr.Zero;Release(transport);transport=IntPtr.Zero;if(initialized){RoUninitialize();initialized=false;}}
 }
}
