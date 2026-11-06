using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using Microsoft.Win32;

static class RtxVoiceSettings {
 const string Settings=@"Software\NVIDIA Corporation\NVIDIA RTX Voice\Settings";
 public static string Executable {get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),@"NVIDIA Corporation\NVIDIA RTX Voice\NVIDIA RTX Voice.exe");}}
 public static bool Installed {get{return File.Exists(Executable);}}
 public static string Source {get{using(var key=Registry.CurrentUser.OpenSubKey(Settings))return key==null?"":Convert.ToString(key.GetValue("MicEndpoint",""));}}
 public static int Strength {get{using(var key=Registry.CurrentUser.OpenSubKey(Settings))return key==null?70:Math.Max(0,Math.Min(100,Convert.ToInt32(key.GetValue("MicDenoisingIntensity",70))));}}
 public static string Friendly(AudioChoice device){int at=device.Name.IndexOf(" · ",StringComparison.Ordinal);return at<0?device.Name:device.Name.Substring(0,at)+" ("+device.Name.Substring(at+3)+")";}
 public static void Configure(AudioChoice source,int strength){
  if(!Installed)throw new InvalidOperationException("Instale o componente RTX Voice antes de ativar o filtro.");if(source==null||source.Name.IndexOf("RTX Voice",StringComparison.OrdinalIgnoreCase)>=0||strength<0||strength>100)throw new ArgumentException("Origem ou intensidade inválida.");
  using(var key=Registry.CurrentUser.CreateSubKey(Settings)){
   string friendly=Friendly(source);bool changed=Convert.ToString(key.GetValue("MicEndpoint",""))!=friendly||Convert.ToInt32(key.GetValue("MicDenoising",0))!=1||Convert.ToInt32(key.GetValue("MicDenoisingIntensity",0))!=strength;
   if(!changed)return;
   if(ConversationRecorder.AnyRecording)throw new InvalidOperationException("Pare e salve a gravação antes de mudar o processamento.");
   var state=NativeHfpBridge.Request("status");if(Convert.ToInt32(state["CallCount"])!=0||Convert.ToInt32(state["Call"])!=0||Convert.ToInt32(state["Setup"])!=0)throw new InvalidOperationException("Aplique a mudança de filtro com o celular ocioso.");
   foreach(var process in Process.GetProcessesByName("NVIDIA RTX Voice")){using(process){if(String.Equals(process.MainModule.FileName,Executable,StringComparison.OrdinalIgnoreCase)){process.Kill();process.WaitForExit(3000);}}}
   key.SetValue("MicEndpoint",friendly,RegistryValueKind.String);key.SetValue("MicDenoising",1,RegistryValueKind.DWord);key.SetValue("MicDenoisingIntensity",strength,RegistryValueKind.DWord);key.SetValue("SpeakerDenoising",0,RegistryValueKind.DWord);
   Process.Start(new ProcessStartInfo(Executable){WorkingDirectory=Path.GetDirectoryName(Executable),UseShellExecute=false,WindowStyle=ProcessWindowStyle.Hidden});
  }
 }
}
