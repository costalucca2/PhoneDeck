using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Threading;
[assembly:System.Reflection.AssemblyVersion("1.13.3.39")]
[assembly:System.Reflection.AssemblyFileVersion("1.13.3.39")]
static class PhoneDeck {
 [STAThread]static void Main(string[] args){
  EmbeddedDependencies.Load();
  if(Array.IndexOf(args,"--native-server")>=0){var server=new Thread(()=>NativeHfpBridge.Serve());server.SetApartmentState(ApartmentState.MTA);server.Start();server.Join();return;}
  UiPreferences.Install();
  PhoneDirectDial.Initialize();
  if(Array.IndexOf(args,"--audio-settings")>=0){Application.EnableVisualStyles();Application.Run(new AudioSettingsForm());return;}
  if(Array.IndexOf(args,"--record-panel")>=0){Application.EnableVisualStyles();Application.Run(new RecordingPanel());return;}
  if(Array.IndexOf(args,"--snapshot")>=0){try{PhoneSnapshot.Save();}catch{Environment.ExitCode=1;}return;}
  if(Array.IndexOf(args,"--setup")>=0){Application.EnableVisualStyles();Application.Run(new PhoneSetupForm());return;}
  if(args.Length==0||Array.IndexOf(args,"--ui")>=0){if(PhoneDeckPresentation.ActivateExisting())return;Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new PhoneDeckWindow(Array.IndexOf(args,"--ui-capture")>=0));return;}
  if(args.Length==2&&args[0]=="--notice"){AudioActions.ShowNotice(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(args[1])));return;}
  bool first;using(var mutex=new Mutex(true,"Local\\PhoneDeck",out first)){if(first)Run(args);else Environment.ExitCode=2;}
 }
 static void Run(string[] args){try{
  var root=AppDomain.CurrentDomain.BaseDirectory;
  if(Array.IndexOf(args,"--native-stop")>=0){NativeHfpBridge.Request("stop");return;}
  if(Array.IndexOf(args,"--native-audio-setup")>=0){var prepared=NativeHfpBridge.Request("audio");File.WriteAllText(Path.Combine(root,"native-audio.local.txt"),Convert.ToString(prepared["Message"]));return;}
  if(Array.IndexOf(args,"--presentation-test")>=0){PhoneDeckPresentation.Restore(args,false);return;}
  if(Array.IndexOf(args,"--discord-microfone")>=0){DiscordMic.Toggle();return;}
  if(AudioActions.Handle(args))return;
  if(Array.IndexOf(args,"--answer-check")>=0){var state=NativeHfpBridge.Request("status");bool incoming=Convert.ToInt32(state["Setup"])==1&&Convert.ToInt32(state["Call"])==0&&Convert.ToInt32(state["Held"])==0;File.WriteAllText(Path.Combine(root,"answer-status.local.txt"),incoming?"Single incoming call available; no action sent.":"No single incoming call; no action sent.");return;}
  if(Array.IndexOf(args,"--atender")>=0){NativeHfpBridge.Request("answer");return;}
  if(Array.IndexOf(args,"--desligar")>=0){NativeHfpBridge.Request("hangup");return;}
  int index=Array.IndexOf(args,"--contato");string id=index>=0&&index+1<args.Length?args[index+1]:null;
  if(index>=0&&(id==null||!Regex.IsMatch(id,@"^[a-z]{2,24}$")))throw new Exception("Contato inválido.");
  int ni=Array.IndexOf(args,"--number");string direct=ni>=0&&ni+1<args.Length?args[ni+1]:null;
  if(id==null&&direct==null)throw new Exception("Selecione um contato ou informe um número.");
  string number=direct??File.ReadAllText(Path.Combine(root,"telefones",id+".txt")).Trim();
  if(!Regex.IsMatch(number,@"\A(?:\+[1-9][0-9]{7,14}|[0-9]{8,14})\z"))throw new Exception("Número de telefone inválido.");
  if(Array.IndexOf(args,"--check")>=0||Array.IndexOf(args,"--inspect")>=0){var state=NativeHfpBridge.Request("status");File.WriteAllText(Path.Combine(root,"native-check.local.txt"),Convert.ToString(state["Message"])+" Nenhuma chamada executada.");return;}
  NativeHfpBridge.Request("dial",number);
 }catch(Exception ex){AudioActions.Notice(ex.GetBaseException().Message);Environment.ExitCode=1;}finally{if(PhoneDeckPresentation.Needed(args)){try{PhoneDeckPresentation.Restore(args,false);}catch{}}}}
}
