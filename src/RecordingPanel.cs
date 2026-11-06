using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

sealed class RecordingPanel:Form {
 readonly DeckButton record=new DeckButton{Text="Gravar",Fill=DeckTheme.Green},folder=new DeckButton{Text="Pasta"};
 readonly Label state=DeckTheme.Label("Gravação manual",10,DeckTheme.Text,true),time=DeckTheme.Label("00:00",12,DeckTheme.Dim);
 readonly System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer();ConversationRecorder recorder;bool busy,closing,manuallyPlaced;DateTime lastAnchor;
 public bool Active {get{return recorder!=null&&recorder.Recording;}}
 [DllImport("user32.dll")]static extern bool ReleaseCapture();[DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr h,uint m,IntPtr w,IntPtr l);
 delegate bool WindowCallback(IntPtr window,IntPtr data);
 [StructLayout(LayoutKind.Sequential)]struct WindowRect {public int Left,Top,Right,Bottom;}
 [DllImport("user32.dll")]static extern bool EnumWindows(WindowCallback callback,IntPtr data);
 [DllImport("user32.dll")]static extern bool IsWindowVisible(IntPtr window);
 [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr window,out uint pid);
 [DllImport("user32.dll")]static extern bool GetWindowRect(IntPtr window,out WindowRect rect);
 public RecordingPanel(){Text="PhoneDeck · Gravação";Name="PhoneDeckRecorder";FormBorderStyle=FormBorderStyle.None;ClientSize=new Size(320,128);BackColor=DeckTheme.Surface;TopMost=true;ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;
  var area=Screen.PrimaryScreen.WorkingArea;Location=new Point(area.Right-Width-20,area.Top+180);
  var title=DeckTheme.Label("PhoneDeck",9,DeckTheme.Dim,true);title.SetBounds(12,7,210,18);Controls.Add(title);title.MouseDown+=(s,e)=>{if(e.Button==MouseButtons.Left){manuallyPlaced=true;ReleaseCapture();SendMessage(Handle,0xA1,(IntPtr)2,IntPtr.Zero);}};
  state.SetBounds(12,29,216,22);time.SetBounds(235,27,75,24);Controls.Add(state);Controls.Add(time);
  var close=new DeckButton{Text="×",Ghost=true,Size=new Size(26,24),Location=new Point(282,4)};Controls.Add(close);close.Click+=(s,e)=>Close();
  record.Name="ConversationRecordToggle";record.SetBounds(12,58,190,38);folder.SetBounds(210,58,98,38);Controls.Add(record);Controls.Add(folder);
  var hint=DeckTheme.Label("Microfone + som da saída escolhida",8,DeckTheme.Dim);hint.SetBounds(12,105,296,17);Controls.Add(hint);
  record.Click+=async(s,e)=>await Toggle();folder.Click+=(s,e)=>{string path=recorder==null?ConversationRecorder.Folder:Path.GetDirectoryName(recorder.FilePath);Directory.CreateDirectory(path);Process.Start(new ProcessStartInfo(path){UseShellExecute=true});};
  timer.Interval=250;timer.Tick+=(s,e)=>{RefreshState();if(!manuallyPlaced&&(DateTime.UtcNow-lastAnchor).TotalSeconds>=2)AnchorToCall();};timer.Start();Shown+=(s,e)=>AnchorToCall();FormClosing+=async(s,e)=>{if(Active&&!closing){e.Cancel=true;await Stop("panel_closed");closing=true;Close();}};FormClosed+=(s,e)=>timer.Dispose();RefreshState();UiPreferences.Apply(this);
 }
 public async Task Toggle(){if(busy)return;busy=true;record.Enabled=false;try{if(Active)await Stop();else{
   if(ConversationRecorder.AnyRecording)throw new InvalidOperationException("Já existe uma gravação no PhoneDeck.");
   var input=AudioConfiguration.Devices(1).Single(d=>d.Selected);var output=AudioConfiguration.Devices(0).Single(d=>d.Selected);
   var context=await Task.Run(()=>CallAudit.RecordingContext());if(IsDisposed||!Visible)return;context["InputName"]=input.Name;context["OutputName"]=output.Name;context["InputMutedAtRequest"]=AudioConfiguration.InputMuted();context["InputLevelAtRequest"]=AudioConfiguration.InputLevel();if(input.Name.IndexOf("RTX Voice",StringComparison.OrdinalIgnoreCase)>=0){context["NoiseSourceConfigured"]=RtxVoiceSettings.Source;context["NoiseStrengthConfigured"]=RtxVoiceSettings.Strength;}
   recorder=new ConversationRecorder(RecordingCapture.Run);recorder.Start(ConversationRecorder.Folder,input.Id,output.Id,context);state.Text=UiPreferences.T("Iniciando gravação…","Starting recording…");
  }}catch(Exception e){state.Text=e.GetBaseException().Message;}finally{busy=false;record.Enabled=true;RefreshState();}}
 public async Task Stop(string reason="manual_stop"){if(recorder!=null){await recorder.Stop(reason);state.Text=String.IsNullOrEmpty(recorder.Error)?UiPreferences.T("Conversa salva","Conversation saved"):recorder.Error;}RefreshState();}
 void RefreshState(){if(recorder!=null){time.Text=recorder.Duration.ToString(@"hh\:mm\:ss");if(Active)state.Text=recorder.Finalizing?UiPreferences.T("Salvando áudio e registro…","Saving audio and log…"):recorder.Started?UiPreferences.T("Gravando conversa","Recording conversation"):UiPreferences.T("Iniciando gravação…","Starting recording…");else if(!String.IsNullOrEmpty(recorder.Error))state.Text=recorder.Error;}record.Text=Active?UiPreferences.T("Parar e salvar","Stop and save"):UiPreferences.T("Gravar","Record");record.Glyph=Active?"\uE71A":"\uE7C8";record.Fill=Active?DeckTheme.Red:DeckTheme.Green;record.Invalidate();}
 void AnchorToCall(){lastAnchor=DateTime.UtcNow;if(manuallyPlaced)return;try{var owners=new System.Collections.Generic.HashSet<uint>();foreach(var process in Process.GetProcessesByName("CallingShellApp")){using(process)owners.Add((uint)process.Id);}if(owners.Count==0)return;
  EnumWindows((window,data)=>{uint pid;GetWindowThreadProcessId(window,out pid);WindowRect r;if(!owners.Contains(pid)||!IsWindowVisible(window)||!GetWindowRect(window,out r)||r.Right-r.Left<120||r.Right-r.Left>1000||r.Bottom-r.Top<50||r.Bottom-r.Top>600)return true;
   var area=Screen.FromRectangle(Rectangle.FromLTRB(r.Left,r.Top,r.Right,r.Bottom)).WorkingArea;int y=r.Bottom+8;if(y+Height>area.Bottom)y=r.Top-Height-8;Location=new Point(Math.Max(area.Left,Math.Min(r.Left,area.Right-Width)),Math.Max(area.Top,Math.Min(y,area.Bottom-Height)));return false;
  },IntPtr.Zero);
 }catch{}}
}
