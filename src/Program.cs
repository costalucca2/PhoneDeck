using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Threading;
using Windows.System;
[assembly:System.Reflection.AssemblyVersion("1.8.0.21")]
[assembly:System.Reflection.AssemblyFileVersion("1.8.0.21")]
static class PhoneDeck {
 [STAThread]static void Main(string[] args){PhoneDirectDial.Initialize();if(Array.IndexOf(args,"--snapshot")>=0){try{PhoneSnapshot.Save();}catch{Environment.ExitCode=1;}return;}if(args.Length==0||Array.IndexOf(args,"--ui")>=0){if(PhoneDeckPresentation.ActivateExisting())return;Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new PhoneDeckWindow(Array.IndexOf(args,"--ui-capture")>=0));return;}if(args.Length==2&&args[0]=="--notice"){AudioActions.ShowNotice(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(args[1])));return;}bool first;using(var mutex=new Mutex(true,"Local\\PhoneDeck",out first)){if(first)Run(args);else Environment.ExitCode=2;}}
 static void Run(string[] args){try{
  var root=AppDomain.CurrentDomain.BaseDirectory;
  if(Array.IndexOf(args,"--presentation-test")>=0){PhoneDeckPresentation.Restore(args,true);return;}
  if(Array.IndexOf(args,"--answer-check")>=0){File.WriteAllText(Path.Combine(root,"answer-status.local.txt"),PhoneDirectDial.Answer(true)?"Incoming answer button available; no action sent.":"No incoming answer button available; no action sent.");return;}
  if(Array.IndexOf(args,"--atender")>=0){PhoneDeckPresentation.RevealBridge();PhoneDirectDial.Answer(false);return;}
  if(Array.IndexOf(args,"--desligar")>=0){PhoneDeckPresentation.RevealBridge();PhoneDirectDial.Hangup();return;}
  if(Array.IndexOf(args,"--discord-microfone")>=0){DiscordMic.Toggle();return;}
  if(AudioActions.Handle(args))return;
  int index=Array.IndexOf(args,"--contato");
  string id=index>=0&&index+1<args.Length?args[index+1]:null;
  if(index>=0&&(id==null||!Regex.IsMatch(id,@"^[a-z]{2,24}$")))throw new Exception("Contato inválido.");
  int ni=Array.IndexOf(args,"--number");
  string direct=ni>=0&&ni+1<args.Length?args[ni+1]:null;
  if(id==null&&direct==null)throw new Exception("Selecione um contato ou informe um número.");
  var number=direct??File.ReadAllText(Path.Combine(root,Path.Combine("telefones",id+".txt"))).Trim();
  if(!Regex.IsMatch(number,@"^(?:\+[1-9][0-9]{7,14}|[0-9]{8,14})$"))throw new Exception("Número de telefone inválido.");
  var uri=new Uri("tel:"+number);
  const string package="Microsoft.YourPhone_8wekyb3d8bbwe";
  if(Array.IndexOf(args,"--inspect")>=0){File.WriteAllText(Path.Combine(root,"discagem-inspecao.txt"),PhoneDirectDial.Inspect(number));return;}
  if(Array.IndexOf(args,"--check")>=0){
   var task=Launcher.QueryUriSupportAsync(uri,LaunchQuerySupportType.Uri,package);
   var limit=DateTime.UtcNow.AddSeconds(5);
   while(task.Status==Windows.Foundation.AsyncStatus.Started&&DateTime.UtcNow<limit)Thread.Sleep(40);
   if(task.Status!=Windows.Foundation.AsyncStatus.Completed||task.GetResults()!=LaunchQuerySupportStatus.Available)throw new Exception("Vincular ao Celular não disponível para chamadas.");
   string check="telefone-check-"+id+".txt";
   File.WriteAllText(Path.Combine(root,check),"OK: Phone Link accepts "+uri+". No call placed. Version 1.8.0.21.");return;
  }
  PhoneDirectDial.Prepare();
  var options=new LauncherOptions{TargetApplicationPackageFamilyName=package};
  var launch=Launcher.LaunchUriAsync(uri,options);
  var deadline=DateTime.UtcNow.AddSeconds(10);
  while(launch.Status==Windows.Foundation.AsyncStatus.Started&&DateTime.UtcNow<deadline)Thread.Sleep(40);
  if(launch.Status!=Windows.Foundation.AsyncStatus.Completed||!launch.GetResults())throw new Exception("Não foi possível abrir a ligação no Vincular ao Celular. Confira a conexão do iPhone.");
  PhoneDirectDial.Dial(number);
 }catch(Exception ex){AudioActions.Notice(ex.Message);Environment.ExitCode=1;}finally{if(PhoneDeckPresentation.Needed(args)){try{PhoneDeckPresentation.Restore(args,Array.IndexOf(args,"--microfone")<0&&Array.IndexOf(args,"--microfone-on")<0&&Array.IndexOf(args,"--microfone-off")<0);}catch{}}}}
}

















