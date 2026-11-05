using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using Windows.ApplicationModel.Calls;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using PhoneDeckNative;

// One persistent, user-local HFP session. Never launches Phone Link or retries a call.
static class NativeHfpBridge {
 static string PipeName {get{return "PhoneDeck.SystemHfp.v2."+WindowsIdentity.GetCurrent().User.Value;}}
 public static Dictionary<string,object> Request(string action,string number=null){
  using(var pipe=new NamedPipeClientStream(".",PipeName,PipeDirection.InOut,PipeOptions.Asynchronous)){
   try{pipe.Connect(150);}catch(TimeoutException){
    if(action=="stop")return new Dictionary<string,object>{{"Ok",true},{"Message","Serviço já encerrado."}};
    NativePackageActivation.Start();
    pipe.Connect(5000);
   }
   var work=Task.Run(()=>{
    using(var writer=new StreamWriter(pipe,new UTF8Encoding(false),1024,true))using(var reader=new StreamReader(pipe,Encoding.UTF8,false,1024,true)){
     writer.AutoFlush=true;writer.WriteLine(new JavaScriptSerializer().Serialize(new Dictionary<string,object>{{"Action",action},{"Number",number??""},{"DeadlineUtcTicks",DateTime.UtcNow.AddSeconds(20).Ticks}}));
     var line=reader.ReadLine();if(line==null||line.Length>16384)throw new IOException("Resposta inválida do serviço PhoneDeck.");
     return new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(line);
    }
   });
   if(!work.Wait(25000))throw new TimeoutException("PhoneDeck não confirmou o comando. Ele não será repetido automaticamente.");
   var result=work.Result;if(!Convert.ToBoolean(result["Ok"]))throw new InvalidOperationException(Convert.ToString(result["Message"]));return result;
  }
 }
 public static void Serve(){
  bool first;using(var owner=new Mutex(true,"Local\\"+PipeName,out first)){
   if(!first)return;
   using(var session=new SystemHfpSession())while(true){
    var security=new PipeSecurity();security.SetAccessRuleProtection(true,false);
    security.AddAccessRule(new PipeAccessRule(WindowsIdentity.GetCurrent().User,PipeAccessRights.FullControl,AccessControlType.Allow));
    using(var pipe=new NamedPipeServerStream(PipeName,PipeDirection.InOut,1,PipeTransmissionMode.Byte,PipeOptions.Asynchronous,4096,4096,security)){
     pipe.WaitForConnection();
     try{
      using(var reader=new StreamReader(pipe,Encoding.UTF8,false,1024,true))using(var writer=new StreamWriter(pipe,new UTF8Encoding(false),1024,true)){
       writer.AutoFlush=true;var read=reader.ReadLineAsync();if(!read.Wait(3000))continue;
       string line=read.Result;if(line==null||line.Length>4096)continue;
       Dictionary<string,object> result;
       try{var request=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(line);var deadline=new DateTime(Convert.ToInt64(request["DeadlineUtcTicks"]),DateTimeKind.Utc);if(deadline>DateTime.UtcNow.AddSeconds(20))deadline=DateTime.UtcNow.AddSeconds(20);result=session.Handle(Convert.ToString(request["Action"]),Convert.ToString(request["Number"]),deadline);}
       catch(Exception e){result=new Dictionary<string,object>{{"Ok",false},{"Message",e.GetBaseException().Message}};}
       writer.WriteLine(new JavaScriptSerializer().Serialize(result));
       if(result.ContainsKey("StopServer")&&Convert.ToBoolean(result["StopServer"]))return;
      }
     }catch(IOException){}catch(ObjectDisposedException){}
    }
   }
  }
 }
}

static class NativePackageActivation {
 [System.Runtime.InteropServices.ComImport,System.Runtime.InteropServices.Guid("45BA127D-10A8-46EA-8AB7-56EA9078943C")]class Manager {}
 [System.Runtime.InteropServices.ComImport,System.Runtime.InteropServices.Guid("2e941141-7f97-4756-ba1d-9decde894a3d"),System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]interface IManager {
  [System.Runtime.InteropServices.PreserveSig]int ActivateApplication([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)]string id,[System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)]string args,uint options,out uint pid);
  void ActivateForFile();void ActivateForProtocol();
 }
 public static void Start(){var manager=(IManager)new Manager();try{uint pid;System.Runtime.InteropServices.Marshal.ThrowExceptionForHR(manager.ActivateApplication("PhoneDeck.NativeBridge_5rzpsgts89xb2!NativeProbe","--native-server",0,out pid));}finally{System.Runtime.InteropServices.Marshal.ReleaseComObject(manager);}}
}

sealed class SystemHfpSession:IDisposable {
 readonly SystemHfp phone=new SystemHfp();
 public Dictionary<string,object> Handle(string action,string number,DateTime deadline){
  if(action=="stop"){
   if(DateTime.UtcNow>=deadline)throw new TimeoutException("Solicitação expirada.");
   if(PhoneCallManager.IsCallActive||PhoneCallManager.IsCallIncoming)throw new InvalidOperationException("Chamada ativa; serviço preservado.");
   // A stop must not establish a Bluetooth connection merely to stop it.
   if(phone.IsConnected&&phone.CallCount()!=0)throw new InvalidOperationException("Chamada detectada; serviço preservado.");
   return new Dictionary<string,object>{{"Ok",true},{"StopServer",true},{"Message","Serviço encerrado."}};
  }
  var result=new SystemHfpDispatch(phone).Execute(action,number,deadline);
  result["AudioPrepared"]=phone.AudioPrepared;
  if(action=="status"&&phone.AudioPrepared)result["Message"]="Controle nativo e rota de voz preparados; conversa ainda não validada.";
  return result;
 }
 public void Dispose(){phone.Dispose();}
}

sealed class NativeHfpSession:IDisposable {
 BluetoothDevice device;RfcommDeviceService service;StreamSocket socket;
 HfpStreamChannel channel;HfpProtocol protocol;DeadlineAtChannel timedChannel;
 public Dictionary<string,object> Handle(string action,string number,DateTime deadline){
  if(DateTime.UtcNow>=deadline)throw new TimeoutException("Prazo do comando expirou antes do atendimento.");
  if(timedChannel!=null)timedChannel.DeadlineUtc=deadline;
  if(action=="stop"){
   if(PhoneCallManager.IsCallActive||PhoneCallManager.IsCallIncoming||(protocol!=null&&!protocol.Status().Idle))throw new InvalidOperationException("Há chamada ativa ou recebida; conexão preservada.");
   Dispose();return new Dictionary<string,object>{{"Ok",true},{"StopServer",true},{"Message","Serviço encerrado sem chamada."}};
  }
  if(action!="status"&&action!="dial"&&action!="answer"&&action!="hangup")throw new ArgumentException("Comando PhoneDeck inválido.");
  if(action=="dial"&&!Regex.IsMatch(number??"",@"\A(?:\+[1-9][0-9]{7,14}|[0-9]{8,14})\z"))throw new ArgumentException("Número inválido.");
  try{
   if(protocol==null)Connect(deadline);
   HfpState state=new HfpCommandDispatch(protocol).Execute(action,number);
   var result=new Dictionary<string,object>{{"Ok",true},{"Backend","HFP direto"},{"Device",device.Name},{"Connected",true},{"AudioValidated",false},{"Message",action=="status"?"Controle Bluetooth conectado; áudio no PC não validado.":"Comando confirmado pelo telefone; confira a chamada."}};
   if(state!=null){result["Call"]=state.Call;result["Setup"]=state.Setup;result["Held"]=state.Held;}
   return result;
  }catch(IOException){Dispose();throw;}catch(TimeoutException){Dispose();throw;}
 }
 void Connect(DateTime deadline){
  if(PhoneCallManager.IsCallActive||PhoneCallManager.IsCallIncoming)throw new InvalidOperationException("Há chamada ativa ou recebida. A conexão HFP não será reiniciada. Abra o PhoneDeck com o telefone ocioso para conectar o controle direto.");
  Dispose();
  try{
   var paired=HfpStreamChannel.Get(DeviceInformation.FindAllAsync(BluetoothDevice.GetDeviceSelectorFromPairingState(true)));
   var candidates=new List<Tuple<BluetoothDevice,RfcommDeviceService>>();
   try{
    foreach(var info in paired){
     BluetoothDevice candidate=null;bool retained=false;
     try{
      candidate=HfpStreamChannel.Get(BluetoothDevice.FromIdAsync(info.Id));if(candidate==null)continue;
      var found=HfpStreamChannel.Get(candidate.GetRfcommServicesForIdAsync(RfcommServiceId.FromUuid(new Guid("0000111f-0000-1000-8000-00805f9b34fb")),BluetoothCacheMode.Uncached));
      if(found.Error!=BluetoothError.Success)continue;
      foreach(var entry in found.Services){candidates.Add(Tuple.Create(candidate,entry));retained=true;}
     }catch(Exception){ /* Non-phone paired interfaces are not gateways. */ }
     finally{if(candidate!=null&&!retained)candidate.Dispose();}
    }
    if(candidates.Count!=1)throw new InvalidOperationException(candidates.Count==0?"Nenhum celular HFP disponível. Confira o Bluetooth e o pareamento.":"Há mais de um gateway HFP. Conecte somente o celular desejado antes de usar PhoneDeck.");
    device=candidates[0].Item1;service=candidates[0].Item2;candidates.Clear();
   }finally{foreach(var item in candidates){item.Item2.Dispose();item.Item1.Dispose();}}
   if(PhoneCallManager.IsCallActive||PhoneCallManager.IsCallIncoming)throw new InvalidOperationException("Chamada detectada durante a conexão; operação cancelada.");
   socket=new StreamSocket();var operation=socket.ConnectAsync(service.ConnectionHostName,service.ConnectionServiceName,SocketProtectionLevel.BluetoothEncryptionWithAuthentication);HfpStreamChannel.Wait(operation,10000);operation.GetResults();
   channel=new HfpStreamChannel(socket);timedChannel=new DeadlineAtChannel(channel,deadline);protocol=new HfpProtocol(timedChannel);protocol.Initialize();
  }catch{Dispose();throw;}
 }
 public void Dispose(){protocol=null;timedChannel=null;if(channel!=null){channel.Dispose();channel=null;}if(socket!=null){socket.Dispose();socket=null;}if(service!=null){service.Dispose();service=null;}if(device!=null){device.Dispose();device=null;}}
}

sealed class HfpStreamChannel:IAtChannel,IDisposable {
 readonly DataWriter writer;readonly DataReader reader;
 public HfpStreamChannel(StreamSocket socket){writer=new DataWriter(socket.OutputStream);reader=new DataReader(socket.InputStream);reader.InputStreamOptions=InputStreamOptions.Partial;}
 public static void Wait(IAsyncInfo operation,int milliseconds){
  var until=DateTime.UtcNow.AddMilliseconds(milliseconds);while(operation.Status==AsyncStatus.Started&&DateTime.UtcNow<until)Thread.Sleep(15);
  if(operation.Status==AsyncStatus.Started){operation.Cancel();throw new TimeoutException("O celular não respondeu a tempo. O comando não será repetido.");}
  if(operation.Status!=AsyncStatus.Completed){var error=operation.ErrorCode;throw new IOException("Falha no transporte Bluetooth: "+operation.Status+(error==null?"":" (0x"+error.HResult.ToString("X8")+"): "+error.Message),error);}
 }
 public static T Get<T>(IAsyncOperation<T> operation){Wait(operation,7000);return operation.GetResults();}
 public string Execute(string command){
  writer.WriteBytes(Encoding.ASCII.GetBytes(command+"\r"));var write=writer.StoreAsync();Wait(write,3000);write.GetResults();
  var buffer=new StringBuilder();var until=DateTime.UtcNow.AddSeconds(5);
  while(DateTime.UtcNow<until){var load=reader.LoadAsync(2048);Wait(load,5000);uint count=load.GetResults();if(count==0)throw new IOException("O celular encerrou o canal Bluetooth.");var bytes=new byte[count];reader.ReadBytes(bytes);buffer.Append(Encoding.ASCII.GetString(bytes));if(buffer.Length>32768)throw new IOException("Resposta HFP excedeu o limite.");string response=buffer.ToString();if(Regex.IsMatch(response,@"(?:^|[\r\n])(?:ERROR|\+CME ERROR[^\r\n]*)(?:[\r\n]|$)"))throw new InvalidOperationException("O telefone recusou o comando. Nenhuma nova tentativa será feita.");if(Regex.IsMatch(response,@"(?:^|[\r\n])OK(?:[\r\n]|$)"))return response;}
  throw new TimeoutException("Sem confirmação do telefone; comando não repetido.");
 }
 public void Dispose(){writer.DetachStream();reader.DetachStream();writer.Dispose();reader.Dispose();}
}
