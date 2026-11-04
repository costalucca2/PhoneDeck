using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Automation;
static class PhoneDirectDial {
 [DllImport("user32.dll")] static extern bool SetProcessDpiAwarenessContext(IntPtr context);
 public static void Initialize(){SetProcessDpiAwarenessContext(new IntPtr(-4));}
 [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hwnd,out uint pid);
 [DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
 [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hwnd);
 [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hwnd,int command);
 [StructLayout(LayoutKind.Sequential)] struct Point {public int x,y;}
 [DllImport("user32.dll")] static extern bool SetCursorPos(int x,int y);
 [DllImport("user32.dll")] static extern bool GetCursorPos(out Point point);
 [DllImport("user32.dll")] static extern IntPtr WindowFromPoint(Point point);
 [DllImport("user32.dll")] static extern void mouse_event(uint flags,uint dx,uint dy,uint data,UIntPtr extra);
 static void Click(AutomationElement window,AutomationElement button,Func<bool> validate=null){
  var hwnd=new IntPtr(window.Current.NativeWindowHandle);
  ShowWindow(hwnd,9);SetForegroundWindow(hwnd);Thread.Sleep(150);
  uint pid;GetWindowThreadProcessId(GetForegroundWindow(),out pid);
  if(pid!=(uint)window.Current.ProcessId)throw new Exception("Vincular ao Celular não recebeu foco. Nenhum clique foi enviado.");
  var rect=button.Current.BoundingRectangle;
  if(button.Current.IsOffscreen||rect.IsEmpty||rect.Width<2||rect.Height<2)throw new Exception("O botão da chamada não está visível.");
  var target=new Point{x=(int)(rect.Left+rect.Width/2),y=(int)(rect.Top+rect.Height/2)};Point previous;GetCursorPos(out previous);
  GetWindowThreadProcessId(WindowFromPoint(target),out pid);
  if(pid!=(uint)window.Current.ProcessId)throw new Exception("O botão da chamada está coberto. Nenhum clique foi enviado.");
  foreach(int key in new[]{0x10,0x11,0x12,0x5B,0x5C})if((GetAsyncKeyState(key)&0x8000)!=0)throw new Exception("Solte as teclas de atalho e tente novamente.");
  if(!SetCursorPos(target.x,target.y))throw new Exception("Não foi possível alcançar o botão da chamada.");
  try{GetWindowThreadProcessId(WindowFromPoint(target),out pid);if(pid!=(uint)window.Current.ProcessId)throw new Exception("A janela mudou. Clique cancelado.");if(validate!=null&&!validate())throw new Exception("O número mudou. Clique cancelado.");mouse_event(2,0,0,0,UIntPtr.Zero);mouse_event(4,0,0,0,UIntPtr.Zero);}finally{SetCursorPos(previous.x,previous.y);}
 }
 public static void Hangup(){
  foreach(var process in Process.GetProcessesByName("PhoneExperienceHost")){
   var windows=AutomationElement.RootElement.FindAll(TreeScope.Children,new PropertyCondition(AutomationElement.ProcessIdProperty,process.Id));
   foreach(AutomationElement w in windows){
    var buttons=w.FindAll(TreeScope.Descendants,new PropertyCondition(AutomationElement.ControlTypeProperty,ControlType.Button));
    foreach(AutomationElement b in buttons){
     if(!b.Current.IsEnabled||b.Current.IsOffscreen)continue;
     var name=b.Current.Name??"";
     if(Regex.IsMatch(name,@"^(Encerrar chamada|Desligar chamada|Terminar chamada|Finalizar chamada|End call|Hang up)(\s*\(.*\))?$",RegexOptions.IgnoreCase)){
      Click(w,b);Log("End-call button activated once in Phone Link.");return;
     }
    }
   }
  }
  throw new Exception("Nenhuma janela de chamada com botão de desligar foi encontrada no Vincular ao Celular.");
 }
 static void PressCall(AutomationElement window,AutomationElement button,AutomationElement text,AutomationElement sub,string number){
  if(!button.Current.IsEnabled||(!Matches(Text(text),number)&&!Matches(Text(sub),number)))throw new Exception("O discador mudou. Ligação cancelada.");
  Click(window,button,()=>button.Current.IsEnabled&&(Matches(Text(text),number)||Matches(Text(sub),number)));
 }
 static void Log(string message){File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"discagem-direta-status.txt"),DateTime.Now.ToString("o")+" "+message+Environment.NewLine);}
 public static void Prepare(){
  Log("Preparing Phone Link; no number logged.");
  Process.Start(new ProcessStartInfo("explorer.exe","shell:AppsFolder\\Microsoft.YourPhone_8wekyb3d8bbwe!App"){UseShellExecute=true});
  var deadline=DateTime.UtcNow.AddSeconds(10);
  while(DateTime.UtcNow<deadline){var w=Window();if(w!=null){var dial=Find(w,"DialPaneGrid");if(dial!=null){Log("Dialer ready before URI launch.");Thread.Sleep(500);return;}var tab=Find(w,"CallingNodeAutomationId");object select;if(tab!=null&&tab.TryGetCurrentPattern(SelectionItemPattern.Pattern,out select))((SelectionItemPattern)select).Select();}Thread.Sleep(200);}
  throw new Exception("O Vincular ao Celular ainda não abriu o discador. Confira a conexão do iPhone e tente novamente.");
 }
 static AutomationElement Find(AutomationElement root,string id){return root.FindFirst(TreeScope.Descendants,new PropertyCondition(AutomationElement.AutomationIdProperty,id));}
 public static string Digits(string text){return Regex.Replace(text??"","[^0-9]","");}
 static string National(string digits){if(digits.StartsWith("0")&&(digits.Length==13||digits.Length==14))digits=digits.Substring(3);if(digits.StartsWith("55")&&(digits.Length==12||digits.Length==13))digits=digits.Substring(2);return digits;}
 public static bool Matches(string displayed,string number){string a=National(Digits(displayed)),b=National(Digits(number));return a.Length>=8&&a==b;}
 static string Text(AutomationElement e){if(e==null)return "";string text=e.Current.Name;object p;if(String.IsNullOrEmpty(text)&&e.TryGetCurrentPattern(TextPattern.Pattern,out p))text=((TextPattern)p).DocumentRange.GetText(128);return text;}
 static AutomationElement Window(){foreach(var p in Process.GetProcessesByName("PhoneExperienceHost")){try{if(p.MainWindowHandle!=IntPtr.Zero)return AutomationElement.FromHandle(p.MainWindowHandle);}catch{}}return null;}
 public static string Inspect(string number){var w=Window();if(w==null)return "Phone Link window unavailable";var dial=Find(w,"DialPaneGrid");if(dial==null)return "Dialer unavailable";var button=Find(dial,"ButtonCall");bool match=Matches(Text(Find(dial,"AccumulatorTextBlock")),number)||Matches(Text(Find(dial,"AccumulatorSubtextBlock")),number);return "Phone Link dialer found; call button="+(button!=null)+"; number matches="+match+"; no call placed";}
 public static void Dial(string number){
  // Only this helper is terminated if an inaccessible Phone Link window stops responding.
  using(var timeout=new Timer(_=>Environment.Exit(3),null,25000,Timeout.Infinite)){
   var deadline=DateTime.UtcNow.AddSeconds(12);
   while(DateTime.UtcNow<deadline){
    var w=Window();if(w!=null){
     var dial=Find(w,"DialPaneGrid");
     if(dial!=null){
      var text=Find(dial,"AccumulatorTextBlock");var sub=Find(dial,"AccumulatorSubtextBlock");
      if(Matches(Text(text),number)||Matches(Text(sub),number)){
       var button=Find(dial,"ButtonCall");
       if(button!=null&&button.Current.IsEnabled&&!button.Current.IsOffscreen){
        // Recheck the number immediately before the one permitted invocation.
        if(!Matches(Text(text),number)&&!Matches(Text(sub),number))throw new Exception("O número do discador mudou. Ligação cancelada.");
        // Let the URI update settle before sending a single call request.
        Thread.Sleep(600);
        if(!Matches(Text(text),number)&&!Matches(Text(sub),number))throw new Exception("O número do discador mudou. Ligação cancelada.");
        PressCall(w,button,text,sub,number);
        Log("Native click sent once to verified Phone Link Call button. No automatic retry.");
        Thread.Sleep(1500);
        var after=Window();var afterDial=after==null?null:Find(after,"DialPaneGrid");
        var afterButton=afterDial==null?null:Find(afterDial,"ButtonCall");
        Log("After request: dialer="+(afterDial!=null)+"; call button enabled="+(afterButton!=null&&afterButton.Current.IsEnabled)+". Connection not verified.");return;
       }
      }
     }
    }
    Thread.Sleep(200);
   }
   throw new Exception("Não encontrei o botão Ligar com o número esperado no Vincular ao Celular. Nenhuma confirmação automática foi enviada.");
  }
 }
}


