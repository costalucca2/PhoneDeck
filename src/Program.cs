using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Threading;
using Windows.System;
[assembly:System.Reflection.AssemblyVersion("1.5.0.10")]
[assembly:System.Reflection.AssemblyFileVersion("1.5.0.10")]
static class PhoneDeck {
 [STAThread]static void Main(string[] args){PhoneDirectDial.Initialize();if(args.Length==2&&args[0]=="--notice"){AudioActions.ShowNotice(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(args[1])));return;}bool first;using(var mutex=new Mutex(true,"Local\\PhoneDeck",out first)){if(first)Run(args);}}
 static void Run(string[] args){try{
  var root=AppDomain.CurrentDomain.BaseDirectory;
  if(Array.IndexOf(args,"--answer-check")>=0){File.WriteAllText(Path.Combine(root,"answer-status.local.txt"),PhoneDirectDial.Answer(true)?"Incoming answer button available; no action sent.":"No incoming answer button available; no action sent.");return;}
  if(Array.IndexOf(args,"--atender")>=0){PhoneDirectDial.Answer(false);return;}
  if(Array.IndexOf(args,"--desligar")>=0){PhoneDirectDial.Hangup();return;}
  if(Array.IndexOf(args,"--discord-microfone")>=0){DiscordMic.Toggle();return;}
  if(AudioActions.Handle(args))return;
  int index=Array.IndexOf(args,"--contato");
  string id=index>=0&&index+1<args.Length?args[index+1]:null;
  if(index>=0&&(id==null||!Regex.IsMatch(id,@"^[a-z]{2,24}$")))throw new Exception("Contato inválido.");
  if(id==null)throw new Exception("Use --contato ID, --microfone, --audio-configure ou --desligar.");
  var file=Path.Combine("telefones",id+".txt");
  var number=File.ReadAllText(Path.Combine(root,file)).Trim();
  if(!Regex.IsMatch(number,@"^(?:\+[0-9]{10,15}|0[0-9]{12,13}|[0-9]{8,9})$"))throw new Exception("Número de telefone inválido.");
  var uri=new Uri("tel:"+number);
  const string package="Microsoft.YourPhone_8wekyb3d8bbwe";
  if(Array.IndexOf(args,"--inspect")>=0){File.WriteAllText(Path.Combine(root,"discagem-inspecao.txt"),PhoneDirectDial.Inspect(number));return;}
  if(Array.IndexOf(args,"--check")>=0){
   var task=Launcher.QueryUriSupportAsync(uri,LaunchQuerySupportType.Uri,package);
   var limit=DateTime.UtcNow.AddSeconds(5);
   while(task.Status==Windows.Foundation.AsyncStatus.Started&&DateTime.UtcNow<limit)Thread.Sleep(40);
   if(task.Status!=Windows.Foundation.AsyncStatus.Completed||task.GetResults()!=LaunchQuerySupportStatus.Available)throw new Exception("Vincular ao Celular não disponível para chamadas.");
   string check="telefone-check-"+id+".txt";
   File.WriteAllText(Path.Combine(root,check),"OK: Phone Link accepts "+uri+". No call placed. Version 1.5.0.10.");return;
  }
  PhoneDirectDial.Prepare();
  var options=new LauncherOptions{TargetApplicationPackageFamilyName=package};
  var launch=Launcher.LaunchUriAsync(uri,options);
  var deadline=DateTime.UtcNow.AddSeconds(10);
  while(launch.Status==Windows.Foundation.AsyncStatus.Started&&DateTime.UtcNow<deadline)Thread.Sleep(40);
  if(launch.Status!=Windows.Foundation.AsyncStatus.Completed||!launch.GetResults())throw new Exception("Não foi possível abrir a ligação no Vincular ao Celular. Confira a conexão do iPhone.");
  PhoneDirectDial.Dial(number);
 }catch(Exception ex){AudioActions.Notice(ex.Message);Environment.ExitCode=1;}}
}






