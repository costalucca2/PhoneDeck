using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Web.Script.Serialization;

public sealed class CallHistoryRow {
 public string Contact,Number,Action,Outcome="requested",Detail,Journal,SessionId;
 public DateTimeOffset RequestedAt;
}
public sealed class CallHistoryResult {
 public readonly List<CallHistoryRow> Rows=new List<CallHistoryRow>();
 public int Unreadable;public bool Limited;
}
public static class CallHistory {
 public static CallHistoryResult Read(string root){
  var result=new CallHistoryResult();string folder=Path.Combine(root,"chamadas");
  if(!Directory.Exists(folder))return result;
  string[] paths;
  try{paths=Directory.GetDirectories(folder).Where(p=>(File.GetAttributes(p)&FileAttributes.ReparsePoint)==0).OrderByDescending(Directory.GetLastWriteTimeUtc).ToArray();}catch{result.Unreadable++;return result;}
  result.Limited=paths.Length>200;
  foreach(string directory in paths.Take(200)){
   string file=Path.Combine(directory,"eventos.jsonl");if(!File.Exists(file))continue;
   try{
    string text;using(var stream=new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.ReadWrite)){if(stream.Length>2097152)throw new InvalidDataException();using(var reader=new StreamReader(stream,Encoding.UTF8))text=reader.ReadToEnd();}
    // A writer may be appending: only completed lines can be displayed.
    int end=text.LastIndexOf('\n');if(end<0)throw new InvalidDataException();
    string previous=new string('0',64);long sequence=0;var rows=new List<CallHistoryRow>();var pending=new Dictionary<string,CallHistoryRow>();
    foreach(string line in text.Substring(0,end).Split('\n')){
     if(String.IsNullOrWhiteSpace(line))continue;var json=new JavaScriptSerializer();var envelope=json.Deserialize<Dictionary<string,object>>(line);string payload=Convert.ToString(envelope["PayloadJson"]),hash=Convert.ToString(envelope["Hash"]);
     if(Convert.ToString(envelope["PreviousHash"])!=previous||hash!=EvidenceFiles.HashText(previous+"\n"+payload))throw new InvalidDataException();previous=hash;
     var entry=json.Deserialize<Dictionary<string,object>>(payload);if(Convert.ToInt64(entry["Sequence"])!=++sequence)throw new InvalidDataException();string kind=Convert.ToString(entry["Event"]);var data=entry["Data"] as Dictionary<string,object>;if(data==null||!data.ContainsKey("Action"))continue;string action=Convert.ToString(data["Action"]);
     if(kind=="call_command_requested"){
      var context=data["Context"] as Dictionary<string,object>;if(context==null)throw new InvalidDataException();DateTimeOffset time;if(!DateTimeOffset.TryParse(Convert.ToString(entry["Utc"]),out time))throw new InvalidDataException();
      var row=new CallHistoryRow{Contact=Value(context,"ContactName"),Number=Value(context,"NumberDialed"),Action=action,RequestedAt=time,Journal=file,SessionId=Value(context,"CallSessionId")};rows.Add(row);pending[action]=row;
     }else if((kind=="call_command_confirmed"||kind=="call_command_failed")&&pending.ContainsKey(action)){
      var row=pending[action];row.Outcome=kind=="call_command_failed"?"failed":"confirmed";row.Detail=Value(data,"Error");pending.Remove(action);
     }
    }
    if(sequence==0)throw new InvalidDataException();result.Rows.AddRange(rows);
   }catch{result.Unreadable++;}
  }
  var ordered=result.Rows.OrderByDescending(r=>r.RequestedAt).Take(200).ToArray();result.Limited|=result.Rows.Count>200;result.Rows.Clear();result.Rows.AddRange(ordered);return result;
 }
 static string Value(Dictionary<string,object> data,string key){return data.ContainsKey(key)?Convert.ToString(data[key]):"";}
}