using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Web.Script.Serialization;

static class CallAudit {
 static string Root {get{return ConversationRecorder.Folder;}}
 static string Pointer {get{return Path.Combine(Root,"current-call.local.json");}}
 public static Dictionary<string,object> Execute(string action,string number=null,string contactId=null){
  string id=Guid.NewGuid().ToString("N");var context=new Dictionary<string,object>{{"CallSessionId",id},{"Direction",action=="dial"?"outgoing":action=="answer"?"incoming":"unknown"},{"NumberDialed",action=="dial"?number:null},{"ContactId",contactId},{"ContactName",ContactName(contactId)},{"CommandRequestedUtc",DateTimeOffset.UtcNow.ToString("o")},{"NetworkIdentityVerified",false},{"Association","PhoneDeck command context; not network-verified"}};
  if(action=="hangup"){var old=ReadPointer();if(old!=null)context=old;id=Convert.ToString(context["CallSessionId"]);}
  EvidenceJournal journal=null;string warning=null;
  try{journal=new EvidenceJournal(Path.Combine(Root,"chamadas",id,"eventos.jsonl"));journal.Append("call_command_requested",new{Action=action,Context=context});}catch(Exception e){warning=e.Message;}
  Dictionary<string,object> result;
  try{result=NativeHfpBridge.Request(action,number);}catch(Exception e){try{if(journal!=null)journal.Append("call_command_failed",new{Action=action,Error=e.GetBaseException().Message,ConnectedConfirmed=false});}catch{}throw;}
  try{if(journal!=null)journal.Append("call_command_confirmed",new{Action=action,Result=result,ConnectedConfirmed=false,Meaning="Command acknowledged; call connection not established by this event"});if(action!="hangup"){Directory.CreateDirectory(Root);context["CommandConfirmedUtc"]=DateTimeOffset.UtcNow.ToString("o");EvidenceFiles.Json(Pointer,context);}else if(File.Exists(Pointer)){File.Delete(Pointer);}}catch(Exception e){warning=e.Message;}
  if(warning!=null)AudioActions.Notice("Comando de chamada enviado, mas o registro não foi confirmado: "+warning);return result;
 }
 static string ContactName(string id){if(String.IsNullOrEmpty(id))return null;try{string path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"telefones-contatos.json");if(!File.Exists(path))return id;var contacts=new JavaScriptSerializer().Deserialize<List<Dictionary<string,object>>>(File.ReadAllText(path));var found=contacts.FirstOrDefault(c=>c.ContainsKey("Id")&&Convert.ToString(c["Id"])==id);return found!=null&&found.ContainsKey("Name")?Convert.ToString(found["Name"]):id;}catch{return id;}}
 static Dictionary<string,object> ReadPointer(){try{if(!File.Exists(Pointer)||new FileInfo(Pointer).Length>65536)return null;var data=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(File.ReadAllText(Pointer));Guid id;if(!data.ContainsKey("CallSessionId")||!Guid.TryParseExact(Convert.ToString(data["CallSessionId"]),"N",out id))return null;DateTimeOffset requested;if(!DateTimeOffset.TryParse(Convert.ToString(data["CommandRequestedUtc"]),out requested)||DateTimeOffset.UtcNow-requested>TimeSpan.FromHours(24)||requested>DateTimeOffset.UtcNow.AddMinutes(1))return null;return data;}catch{return null;}}
 public static Dictionary<string,object> RecordingContext(){
  var result=new Dictionary<string,object>{{"Association","unknown"},{"NetworkIdentityVerified",false}};
  result["MonitorHfp"]=true;try{var state=NativeHfpBridge.RequestIfRunning("observe");result["ObservedStateAtStart"]=state;if(state==null||!state.ContainsKey("CallCount")||state["CallCount"]==null||Convert.ToInt32(state["CallCount"])!=1){result["Reason"]="No single active call observed; previous contact was not reused";return result;}var pointer=ReadPointer();if(pointer==null){result["Reason"]="Active call without a reliable recent PhoneDeck command context";return result;}
   foreach(string key in new[]{"CallSessionId","Direction","NumberDialed","ContactId","ContactName","CommandRequestedUtc","CommandConfirmedUtc"})if(pointer.ContainsKey(key))result[key]=pointer[key];result["Association"]="Recent local command associated by timing, not by a network call identifier";result["Reason"]="Destination is the number requested in PhoneDeck, not independently verified remote identity";
  }catch(Exception e){result["Reason"]="Status unavailable: "+e.GetBaseException().Message;}
  return result;
 }
 public static object Observe(){try{var state=NativeHfpBridge.RequestIfRunning("observe");return new{Status=state,IdentityVerified=false};}catch(Exception e){return new{Error=e.GetBaseException().Message,IdentityVerified=false};}}
 public static void CopyJournal(string id,string destination){Guid parsed;if(!Guid.TryParseExact(id,"N",out parsed))return;string source=Path.Combine(Root,"chamadas",parsed.ToString("N"),"eventos.jsonl");if(!File.Exists(source))return;EvidenceJournal.CopySnapshot(source,destination);if(!EvidenceJournal.Verify(destination))throw new InvalidDataException("Cópia do registro da chamada incompleta ou inválida.");}
}
