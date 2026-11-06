using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using PhoneDeckAudio;

public sealed class AudioChoice {
 public string Id,Name;public bool Selected;
 public override string ToString(){return Name;}
}
public static class AudioConfiguration {
 static void Free(object value){if(value!=null&&Marshal.IsComObject(value))Marshal.ReleaseComObject(value);}
 public static List<AudioChoice> Devices(int flow){
  var result=new List<AudioChoice>();IDevices ds=null;PhoneDeckAudio.ICollection list=null;IDevice current=null;
  try{ds=(IDevices)new Enumerator();string selected="";if(ds.GetDefaultAudioEndpoint(flow,2,out current)>=0)current.GetId(out selected);
   Marshal.ThrowExceptionForHR(ds.EnumAudioEndpoints(flow,1,out list));uint count;Marshal.ThrowExceptionForHR(list.GetCount(out count));
   for(uint i=0;i<count;i++){IDevice device=null;try{Marshal.ThrowExceptionForHR(list.Item(i,out device));string id;Marshal.ThrowExceptionForHR(device.GetId(out id));var match=System.Text.RegularExpressions.Regex.Match(id,@"\{[0-9a-f-]{36}\}$",System.Text.RegularExpressions.RegexOptions.IgnoreCase);string label="Dispositivo de áudio";using(var key=Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\"+(flow==0?"Render":"Capture")+@"\"+match.Value+@"\Properties")){if(key!=null)label=Convert.ToString(key.GetValue("{a45c254e-df1c-4efd-8020-67d146a850e0},2"))+" · "+Convert.ToString(key.GetValue("{b3f8fa53-0004-438e-9003-51a46e139bfc},6"));}result.Add(new AudioChoice{Id=id,Name=label,Selected=id==selected});}finally{Free(device);}}
  }finally{Free(current);Free(list);Free(ds);}return result;
 }
 static T Volume<T>(Func<IEndpointVolume,T> action){IDevices ds=null;IDevice d=null;object volume=null;try{ds=(IDevices)new Enumerator();Marshal.ThrowExceptionForHR(ds.GetDefaultAudioEndpoint(1,2,out d));Guid iid=typeof(IEndpointVolume).GUID;Marshal.ThrowExceptionForHR(d.Activate(ref iid,23,IntPtr.Zero,out volume));return action((IEndpointVolume)volume);}finally{Free(volume);Free(d);Free(ds);}}
 public static bool InputMuted(){return Volume(v=>{bool muted;Marshal.ThrowExceptionForHR(v.GetMute(out muted));return muted;});}
 public static int InputLevel(){return Volume(v=>{float level;Marshal.ThrowExceptionForHR(v.GetMasterVolumeLevelScalar(out level));return (int)Math.Round(level*100);});}
 public static void SetInput(int level,bool muted){if(level<0||level>100)throw new ArgumentOutOfRangeException("level");Volume(v=>{Guid context=Guid.Empty;Marshal.ThrowExceptionForHR(v.SetMasterVolumeLevelScalar(level/100f,ref context));Marshal.ThrowExceptionForHR(v.SetMute(muted,ref context));return 0;});}
}
