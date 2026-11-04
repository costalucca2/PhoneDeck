using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Web.Script.Serialization;
static class PhoneDeckPresentation {
 delegate bool WindowCallback(IntPtr hwnd,IntPtr data);
 [DllImport("user32.dll")]static extern bool EnumWindows(WindowCallback callback,IntPtr data);
 [DllImport("user32.dll")]static extern bool IsWindowVisible(IntPtr hwnd);
 [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr hwnd,out uint pid);
 [DllImport("user32.dll")]static extern bool ShowWindowAsync(IntPtr hwnd,int command);
 [DllImport("user32.dll")]static extern bool SetForegroundWindow(IntPtr hwnd);
 public static void RevealBridge(){var ids=new HashSet<uint>(Process.GetProcessesByName("PhoneExperienceHost").Select(p=>(uint)p.Id));EnumWindows((h,d)=>{uint pid;GetWindowThreadProcessId(h,out pid);if(ids.Contains(pid)&&IsWindowVisible(h))ShowWindowAsync(h,9);return true;},IntPtr.Zero);Thread.Sleep(200);}
 public static bool ActivateExisting(){var file=System.Windows.Forms.Application.ExecutablePath;foreach(var p in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(file))){try{if(p.Id!=Process.GetCurrentProcess().Id&&p.MainWindowHandle!=IntPtr.Zero&&p.MainWindowTitle=="PhoneDeck"&&String.Equals(p.MainModule.FileName,file,StringComparison.OrdinalIgnoreCase)){ShowWindowAsync(p.MainWindowHandle,9);SetForegroundWindow(p.MainWindowHandle);return true;}}catch{}}return false;}
 public static bool Needed(string[] args){return !args.Contains("--check")&&!args.Contains("--inspect")&&!args.Contains("--answer-check")&&(args.Contains("--contato")||args.Contains("--number")||args.Contains("--atender")||args.Contains("--desligar")||args.Contains("--microfone")||args.Contains("--microfone-on")||args.Contains("--microfone-off"));}
 public static void Restore(string[] args,bool minimizeBridge){
  var root=AppDomain.CurrentDomain.BaseDirectory;int i=Array.IndexOf(args,"--contato");string id=i>=0&&i+1<args.Length?args[i+1]:"";
  File.WriteAllText(Path.Combine(root,"presentation.local.json"),new JavaScriptSerializer().Serialize(new Dictionary<string,object>{{"Contact",id},{"Updated",DateTime.UtcNow.ToString("o")},{"Success",Environment.ExitCode==0}}));
  if(minimizeBridge){var ids=new HashSet<uint>(Process.GetProcessesByName("PhoneExperienceHost").Select(p=>(uint)p.Id));EnumWindows((h,d)=>{uint pid;GetWindowThreadProcessId(h,out pid);if(ids.Contains(pid)&&IsWindowVisible(h))ShowWindowAsync(h,6);return true;},IntPtr.Zero);}
  var file=Path.Combine(root,"PhoneDeck.exe");if(!File.Exists(file))file=System.Windows.Forms.Application.ExecutablePath;
  bool launched=false;var deadline=DateTime.UtcNow.AddSeconds(5);
  while(DateTime.UtcNow<deadline){foreach(var p in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(file))){try{if(p.Id==Process.GetCurrentProcess().Id||p.MainWindowHandle==IntPtr.Zero||p.MainWindowTitle!="PhoneDeck"||!String.Equals(p.MainModule.FileName,file,StringComparison.OrdinalIgnoreCase))continue;ShowWindowAsync(p.MainWindowHandle,9);SetForegroundWindow(p.MainWindowHandle);return;}catch{}}
   if(!launched){Process.Start(new ProcessStartInfo(file,"--ui"){UseShellExecute=true});launched=true;}Thread.Sleep(100);
  }
 }
}


