using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Web.Script.Serialization;

public sealed class CallRecordingInfo {
 public string Folder,Summary,State;public bool Integrity;
 public override string ToString(){return Path.GetFileName(Folder)+(Integrity?"":" · INTEGRIDADE NÃO CONFERIDA");}
}
public static class CallRecordingLookup {
 public static List<CallRecordingInfo> Find(string root,CallHistoryRow row){
  var result=new List<CallRecordingInfo>();Guid id;if(!Guid.TryParseExact(row.SessionId,"N",out id)||!Directory.Exists(root))return result;
  foreach(string folder in Directory.EnumerateDirectories(root,"conversa-*")){
   try{
    if((File.GetAttributes(folder)&FileAttributes.ReparsePoint)!=0)continue;
    string manifest=Path.Combine(folder,"manifesto.json");if(!File.Exists(manifest)||new FileInfo(manifest).Length>1048576)continue;
    var data=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(File.ReadAllText(manifest));var context=data["Context"] as Dictionary<string,object>;if(context==null||!context.ContainsKey("CallSessionId")||Convert.ToString(context["CallSessionId"])!=id.ToString("N"))continue;
    bool valid=EvidenceJournal.VerifyBundle(folder);string summary=Path.Combine(folder,"resumo.txt");result.Add(new CallRecordingInfo{Folder=folder,Integrity=valid,State=Convert.ToString(data["Status"]),Summary=File.Exists(summary)&&new FileInfo(summary).Length<=1048576?File.ReadAllText(summary):"Resumo indisponível."});
   }catch{}
  }
  return result.OrderBy(r=>r.Folder,StringComparer.OrdinalIgnoreCase).ToList();
 }
 public static string ReadLog(string path){
  try{if(!File.Exists(path)||new FileInfo(path).Length>2097152)return "Log indisponível ou acima do limite de leitura.";
   var output=new StringBuilder();using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))using(var reader=new StreamReader(stream,Encoding.UTF8)){string line;while((line=reader.ReadLine())!=null){if(String.IsNullOrWhiteSpace(line))continue;try{var json=new JavaScriptSerializer();var envelope=json.Deserialize<Dictionary<string,object>>(line);var entry=json.Deserialize<Dictionary<string,object>>(Convert.ToString(envelope["PayloadJson"]));output.AppendLine(Convert.ToString(entry["LocalWithOffset"])+" | "+Convert.ToString(entry["Event"]));output.AppendLine(json.Serialize(entry["Data"]));output.AppendLine();}catch{output.AppendLine("Evento incompleto ou ilegível.");}}}return output.ToString();
  }catch(Exception e){return "Log indisponível: "+e.Message;}
 }
}