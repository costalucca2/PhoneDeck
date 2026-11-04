using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
static class DiscordMic {
 [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr window,int command);
 [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr window);
 [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr window,out uint pid);
 public static void Toggle(){
  Process window=null;var deadline=DateTime.UtcNow.AddSeconds(8);bool launched=false;
  while(DateTime.UtcNow<deadline){foreach(var p in Process.GetProcessesByName("Discord")){if(p.MainWindowHandle!=IntPtr.Zero){window=p;break;}}
   if(window!=null)break;
   if(!launched){var updater=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Discord","Update.exe");if(!File.Exists(updater))throw new Exception("Discord desktop não encontrado.");Process.Start(new ProcessStartInfo(updater,"--processStart Discord.exe"){UseShellExecute=true,WindowStyle=ProcessWindowStyle.Hidden});launched=true;}
   Thread.Sleep(200);
  }
  if(window==null)throw new Exception("Discord não abriu uma janela para controlar o microfone.");
  ShowWindow(window.MainWindowHandle,9);SetForegroundWindow(window.MainWindowHandle);Thread.Sleep(200);
  uint pid;GetWindowThreadProcessId(GetForegroundWindow(),out pid);
  if(pid!=(uint)window.Id)throw new Exception("Discord não recebeu foco. Nenhum atalho foi enviado.");
  SendKeys.SendWait("^+m");
  File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"discord-microfone.local.txt"),DateTime.Now.ToString("o")+" Toggle command sent; actual Discord mute state unverified.");
 }
}

