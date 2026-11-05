using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Windows.ApplicationModel.Calls;
using Windows.Devices.Enumeration;
[assembly:System.Reflection.AssemblyFileVersion("0.1.0.38")]
// Executes the existing no-sample KS probe under our own registered identity.
static class NativeAudioAccess {
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern int GetCurrentPackageFullName(ref uint length,StringBuilder name);
 static readonly StringBuilder log=new StringBuilder();
 static void Record(string text){log.AppendLine(text);var root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"PhoneDeck");Directory.CreateDirectory(root);File.WriteAllText(Path.Combine(root,"native-audio-access.local.txt"),log.ToString());}
 static T Get<T>(Windows.Foundation.IAsyncOperation<T> operation){var end=DateTime.UtcNow.AddSeconds(12);while(operation.Status==Windows.Foundation.AsyncStatus.Started&&DateTime.UtcNow<end){Application.DoEvents();Thread.Sleep(20);}if(operation.Status==Windows.Foundation.AsyncStatus.Started){operation.Cancel();throw new TimeoutException("Native audio preparation timed out");}return operation.GetResults();}
 [STAThread]static void Main(){Application.EnableVisualStyles();using(var form=new Form{Text="PhoneDeck · Verificação de áudio",Width=510,Height=135,StartPosition=FormStartPosition.CenterScreen}){
  form.Controls.Add(new Label{Text="Verificando o acesso ao driver de voz.\nNenhuma chamada ou captura de microfone.",Dock=DockStyle.Fill,Padding=new Padding(18)});
  form.Shown+=(sender,args)=>form.BeginInvoke(new Action(()=>{try{
   uint size=0;int identity=GetCurrentPackageFullName(ref size,null);Record("Identity="+identity);if(identity!=122&&identity!=0)throw new Exception("Missing package identity");
   if(PhoneCallManager.IsCallActive||PhoneCallManager.IsCallIncoming)throw new Exception("Call active/incoming; no audio test");
   var found=Get(DeviceInformation.FindAllAsync(PhoneLineTransportDevice.GetDeviceSelector()));Record("Transports="+found.Count);if(found.Count!=1)throw new Exception("Exactly one phone required");
   var transport=PhoneLineTransportDevice.FromId(found[0].Id);var access=Get(transport.RequestAccessAsync());Record("Access="+access);if(access!=DeviceAccessStatus.Allowed)throw new Exception("Transport access not allowed");
   if(!transport.IsRegistered())transport.RegisterApp();Record("Registered="+transport.IsRegistered());Record("NativeConnected="+Get(transport.ConnectAsync()));
   Record("PinProbeExit="+AudioStreamProbe.Probe());Record("NoCallCommands=True;NoSamples=True");
  }catch(Exception e){Record("Error="+e.HResult.ToString("X8")+";"+e.Message);}finally{form.Close();}}));Application.Run(form);
 }}
}
