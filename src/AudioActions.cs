using System;
using System.IO;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Windows.Forms;
using PhoneDeckAudio;
static class AudioActions {
 [ComImport,Guid("870af99c-171d-4f9e-af0d-e63df40c2bc9")] class PolicyClient {}
 [ComImport,Guid("f8679f50-850a-41cf-9c72-430f290290c8"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IPolicy {
  void GetMixFormat();void GetDeviceFormat();void ResetDeviceFormat();void SetDeviceFormat();void GetProcessingPeriod();void SetProcessingPeriod();void GetShareMode();void SetShareMode();void GetPropertyValue();void SetPropertyValue();
  [PreserveSig]int SetDefaultEndpoint([MarshalAs(UnmanagedType.LPWStr)]string id,int role);
 }
 static void Free(object o){if(o!=null&&Marshal.IsComObject(o))Marshal.ReleaseComObject(o);}
 public static int SpeakerLevel(int? percent){IDevices ds=null;IDevice device=null;object volume=null;try{ds=(IDevices)new Enumerator();Marshal.ThrowExceptionForHR(ds.GetDefaultAudioEndpoint(0,1,out device));Guid iid=typeof(IEndpointVolume).GUID;Marshal.ThrowExceptionForHR(device.Activate(ref iid,23,IntPtr.Zero,out volume));var v=(IEndpointVolume)volume;if(percent.HasValue){Guid context=Guid.Empty;Marshal.ThrowExceptionForHR(v.SetMasterVolumeLevelScalar(Math.Max(0,Math.Min(100,percent.Value))/100f,ref context));}float level;Marshal.ThrowExceptionForHR(v.GetMasterVolumeLevelScalar(out level));return (int)Math.Round(level*100);}finally{Free(volume);Free(device);Free(ds);}}
 public static string DeviceLabel(int flow){IDevices ds=null;try{ds=(IDevices)new Enumerator();string id=Endpoint(ds,flow,1);var match=System.Text.RegularExpressions.Regex.Match(id,@"\{[0-9a-f-]{36}\}$",System.Text.RegularExpressions.RegexOptions.IgnoreCase);using(var key=Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\"+(flow==0?"Render":"Capture")+@"\"+match.Value+@"\Properties")){if(key==null)return "Dispositivo padrão";return Convert.ToString(key.GetValue("{a45c254e-df1c-4efd-8020-67d146a850e0},2"))+" · "+Convert.ToString(key.GetValue("{b3f8fa53-0004-438e-9003-51a46e139bfc},6"));}}finally{Free(ds);}}
 static string Endpoint(IDevices ds,int flow,int role){IDevice d=null;try{Marshal.ThrowExceptionForHR(ds.GetDefaultAudioEndpoint(flow,role,out d));string id;Marshal.ThrowExceptionForHR(d.GetId(out id));return id;}finally{Free(d);}}
 public static string Microphone(bool? muted,bool toggle){
  IDevices ds=null;var seen=new HashSet<string>();var results=new List<string>();bool? desired=muted;
  try{ds=(IDevices)new Enumerator();foreach(int role in new[]{1,2}){IDevice d=null;object volume=null;try{
   Marshal.ThrowExceptionForHR(ds.GetDefaultAudioEndpoint(1,role,out d));string id;Marshal.ThrowExceptionForHR(d.GetId(out id));if(!seen.Add(id))continue;
   Guid iid=typeof(IEndpointVolume).GUID;Marshal.ThrowExceptionForHR(d.Activate(ref iid,23,IntPtr.Zero,out volume));var v=(IEndpointVolume)volume;
   bool actual;Marshal.ThrowExceptionForHR(v.GetMute(out actual));if(toggle&&!desired.HasValue)desired=!actual;
   if(desired.HasValue){Guid context=Guid.Empty;Marshal.ThrowExceptionForHR(v.SetMute(desired.Value,ref context));Marshal.ThrowExceptionForHR(v.GetMute(out actual));}
   results.Add(actual?"silenciado":"ligado");
  }finally{Free(volume);Free(d);}}return "Microfone: "+String.Join(" / ",results.ToArray());}finally{Free(ds);}
 }
 public static void Configure(){
  IDevices ds=null;IPolicy policy=null;
  try{ds=(IDevices)new Enumerator();string output=Endpoint(ds,0,1),input=Endpoint(ds,1,1);
   string previous="output="+Endpoint(ds,0,2)+Environment.NewLine+"input="+Endpoint(ds,1,2);
   File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"audio-previous.local.txt"),previous);
   policy=(IPolicy)new PolicyClient();Marshal.ThrowExceptionForHR(policy.SetDefaultEndpoint(output,2));Marshal.ThrowExceptionForHR(policy.SetDefaultEndpoint(input,2));
   if(Endpoint(ds,0,2)!=output||Endpoint(ds,1,2)!=input)throw new Exception("O Windows não confirmou os dispositivos de chamadas.");
   Notice("Chamadas configuradas para os mesmos dispositivos do áudio normal. "+Microphone(null,false));
  }finally{Free(policy);Free(ds);}
 }
 public static bool Handle(string[] args){
  if(Array.IndexOf(args,"--audio-status")>=0){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"audio-status.local.txt"),Microphone(null,false));return true;}
  if(Array.IndexOf(args,"--audio-configure")>=0){Configure();return true;}
  if(Array.IndexOf(args,"--audio-settings")>=0){Process.Start(new ProcessStartInfo("ms-settings:sound"){UseShellExecute=true});return true;}
  bool toggle=Array.IndexOf(args,"--microfone")>=0,on=Array.IndexOf(args,"--microfone-on")>=0,off=Array.IndexOf(args,"--microfone-off")>=0;
  if(toggle||on||off){Notice(Microphone(on?(bool?)false:off?(bool?)true:null,toggle));return true;}return false;
 }
 sealed class Toast:Form {protected override bool ShowWithoutActivation{get{return true;}} protected override CreateParams CreateParams{get{var p=base.CreateParams;p.ExStyle|=0x08000000|0x80;return p;}}}
 public static void Notice(string message){Process.Start(new ProcessStartInfo(Application.ExecutablePath,"--notice "+Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(message))){UseShellExecute=false,CreateNoWindow=true});}
 public static void ShowNotice(string message){
  File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"phonedeck-status.local.log"),DateTime.Now.ToString("o")+" "+message+Environment.NewLine);
  using(var f=new Toast())using(var timer=new System.Windows.Forms.Timer()){
   f.FormBorderStyle=FormBorderStyle.None;f.TopMost=true;f.ShowInTaskbar=false;f.Size=new Size(460,105);f.BackColor=Color.FromArgb(25,25,28);
   var label=new Label{Text=message,ForeColor=Color.White,Dock=DockStyle.Fill,Padding=new Padding(16),Font=new Font("Segoe UI",11)};f.Controls.Add(label);
   var area=Screen.PrimaryScreen.WorkingArea;f.StartPosition=FormStartPosition.Manual;f.Location=new Point(area.Right-f.Width-20,area.Bottom-f.Height-20);
   timer.Interval=4500;timer.Tick+=(s,e)=>f.Close();timer.Start();Application.Run(f);
  }
 }
}

