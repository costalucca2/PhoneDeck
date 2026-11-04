using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Automation;
static class PhoneSnapshot {
 static AutomationElement Find(AutomationElement root,string id){return root.FindFirst(TreeScope.Descendants,new PropertyCondition(AutomationElement.AutomationIdProperty,id));}
 static string Name(AutomationElement e){return e==null?"":e.Current.Name;}
 public static void Save(){
  using(var timeout=new Timer(_=>Environment.Exit(3),null,12000,Timeout.Infinite)){
   var result=new Dictionary<string,object>();var rows=new List<Dictionary<string,string>>();
   result["Updated"]=DateTime.Now.ToString("o");result["Device"]="";result["Connection"]="Sem leitura";result["Recent"]=rows;
   foreach(var p in Process.GetProcessesByName("PhoneExperienceHost")){
    if(p.MainWindowHandle==IntPtr.Zero)continue;
    var w=AutomationElement.FromHandle(p.MainWindowHandle);
    result["Device"]=Name(Find(w,"PhoneNameTextBlock"));var state=Name(Find(w,"ConnectivityStatusTextBlock"));if(state.Length>0)result["Connection"]=state;
    var history=Find(w,"CallLogsGrid");if(history!=null){var texts=history.FindAll(TreeScope.Descendants,new OrCondition(new PropertyCondition(AutomationElement.AutomationIdProperty,"CallLogsDisplayName"),new PropertyCondition(AutomationElement.AutomationIdProperty,"CallLogTime")));Dictionary<string,string> row=null;foreach(AutomationElement e in texts){if(e.Current.AutomationId=="CallLogsDisplayName"){if(rows.Count>=16)break;row=new Dictionary<string,string>{{"Name",e.Current.Name},{"Time",""}};rows.Add(row);}else if(row!=null)row["Time"]=e.Current.Name;}}
    break;
   }
   var file=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"phone-snapshot.local.json");var temp=file+".tmp";File.WriteAllText(temp,new JavaScriptSerializer().Serialize(result));if(File.Exists(file))File.Delete(file);File.Move(temp,file);
  }
 }
}
