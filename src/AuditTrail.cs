using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Globalization;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Web.Script.Serialization;

public static class EvidenceFiles {
 public static string Hash(string file){using(var stream=new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.Read))using(var hash=SHA256.Create())return Hex(hash.ComputeHash(stream));}
 public static string HashText(string text){using(var hash=SHA256.Create())return Hex(hash.ComputeHash(Encoding.UTF8.GetBytes(text)));}
 static string Hex(byte[] value){return BitConverter.ToString(value).Replace("-","").ToLowerInvariant();}
 public static void Json(string path,object value){var data=Encoding.UTF8.GetBytes(new JavaScriptSerializer().Serialize(value));using(var stream=new FileStream(path,FileMode.Create,FileAccess.Write,FileShare.Read)){stream.Write(data,0,data.Length);stream.Flush(true);}}
}

// Hashes make changes detectable relative to a preserved digest, not impossible.
public sealed class EvidenceJournal {
 readonly string path;
 public EvidenceJournal(string file){path=file;Directory.CreateDirectory(Path.GetDirectoryName(file));}
 public string FilePath {get{return path;}}
 static string Zero {get{return new string('0',64);}}
 static string LockName(string file){return "Local\\PhoneDeck.Evidence."+EvidenceFiles.HashText(Path.GetFullPath(file).ToUpperInvariant());}
 public void Append(string kind,object details){using(var mutex=new Mutex(false,LockName(path))){bool held=false;try{try{held=mutex.WaitOne(3000);}catch(AbandonedMutexException){held=true;}if(!held)throw new IOException("Registro de eventos ocupado.");
  string previous=Zero;long sequence=0;
  using(var file=new FileStream(path,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.Read)){
   if(file.Length>0){file.Position=Math.Max(0,file.Length-65536);using(var reader=new StreamReader(file,Encoding.UTF8,false,4096,true)){string tail=reader.ReadToEnd();string last=tail.Split('\n').LastOrDefault(line=>!String.IsNullOrWhiteSpace(line));if(last==null)throw new InvalidDataException("Registro sem evento final válido.");var envelope=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(last);string payload=Convert.ToString(envelope["PayloadJson"]);previous=Convert.ToString(envelope["Hash"]);if(previous!=EvidenceFiles.HashText(Convert.ToString(envelope["PreviousHash"])+"\n"+payload))throw new InvalidDataException("Integridade do último evento inválida.");var parsed=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(payload);sequence=Convert.ToInt64(parsed["Sequence"]);}}
   var now=DateTimeOffset.Now;var record=new Dictionary<string,object>{{"Sequence",sequence+1},{"Utc",now.UtcDateTime.ToString("o",CultureInfo.InvariantCulture)},{"LocalWithOffset",now.ToString("o",CultureInfo.InvariantCulture)},{"TimeZoneId",TimeZoneInfo.Local.Id},{"MonotonicTicks",Stopwatch.GetTimestamp()},{"MonotonicFrequency",Stopwatch.Frequency},{"ProcessId",Process.GetCurrentProcess().Id},{"Event",kind},{"Data",details}};
   string json=new JavaScriptSerializer().Serialize(record);var row=new Dictionary<string,object>{{"PayloadJson",json},{"PreviousHash",previous},{"Hash",EvidenceFiles.HashText(previous+"\n"+json)}};byte[] bytes=Encoding.UTF8.GetBytes(new JavaScriptSerializer().Serialize(row)+"\n");file.Position=file.Length;file.Write(bytes,0,bytes.Length);file.Flush(true);
  }
 }finally{if(held)mutex.ReleaseMutex();}}}
 public static bool Verify(string file){try{string previous=Zero;long expected=1;using(var reader=new StreamReader(file,Encoding.UTF8)){string line;while((line=reader.ReadLine())!=null){if(line.Length==0)continue;var envelope=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(line);string payload=Convert.ToString(envelope["PayloadJson"]),hash=Convert.ToString(envelope["Hash"]);if(Convert.ToString(envelope["PreviousHash"])!=previous||hash!=EvidenceFiles.HashText(previous+"\n"+payload))return false;var data=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(payload);if(Convert.ToInt64(data["Sequence"])!=expected++)return false;previous=hash;}}return expected>1;}catch{return false;}}
 public static void CopySnapshot(string source,string destination){using(var mutex=new Mutex(false,LockName(source))){bool held=false;try{try{held=mutex.WaitOne(3000);}catch(AbandonedMutexException){held=true;}if(!held)throw new IOException("Registro de chamada ocupado.");File.Copy(source,destination,false);}finally{if(held)mutex.ReleaseMutex();}}}
 public static bool VerifyBundle(string folder){try{string manifestPath=Path.Combine(folder,"manifesto.json");if(File.ReadAllText(Path.Combine(folder,"manifesto.sha256")).Trim()!=EvidenceFiles.Hash(manifestPath))return false;var manifest=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(File.ReadAllText(manifestPath));foreach(var field in new[]{"Audio","Events","Summary","CallJournal"}){if(!manifest.ContainsKey(field)||manifest[field]==null){if(field!="CallJournal")return false;continue;}var item=(Dictionary<string,object>)manifest[field];string name=Convert.ToString(item["File"]);if(String.IsNullOrEmpty(name)||name!=Path.GetFileName(name)||Path.IsPathRooted(name)||name=="."||name=="..")return false;if(field=="Audio"&&item.ContainsKey("Exists")&&!Convert.ToBoolean(item["Exists"])){if(File.Exists(Path.Combine(folder,name)))return false;continue;}if(EvidenceFiles.Hash(Path.Combine(folder,name))!=Convert.ToString(item["Sha256"]))return false;if(field=="CallJournal"&&!Verify(Path.Combine(folder,name)))return false;}return Verify(Path.Combine(folder,"eventos.jsonl"));}catch{return false;}}
}

public sealed class RecordingEvidence {
 readonly EvidenceJournal journal;readonly string folder,id;readonly Dictionary<string,object> context;readonly DateTimeOffset requested=DateTimeOffset.Now;readonly long startedTicks=Stopwatch.GetTimestamp();
 public RecordingEvidence(string directory,string recordingId,Dictionary<string,object> metadata){folder=directory;id=recordingId;context=metadata??new Dictionary<string,object>{{"Association","unknown"},{"Reason","No call metadata supplied"}};journal=new EvidenceJournal(Path.Combine(folder,"eventos.jsonl"));journal.Append("recording_requested",new{RecordingId=id,Context=context,ClockSource="Windows system clock",ClockSynchronizationCertified=false});}
 public void Event(string name,object data){journal.Append(name,data);}
 public void Finish(string audio,string error,double duration,object sourceStatistics){
  var ended=DateTimeOffset.Now;double monotonic=(Stopwatch.GetTimestamp()-startedTicks)*1000.0/Stopwatch.Frequency;bool exists=File.Exists(audio);string audioHash=exists?EvidenceFiles.Hash(audio):null;
  journal.Append("recording_finalized",new{RecordingId=id,AudioFile=Path.GetFileName(audio),AudioSha256=audioHash,AudioBytes=exists?new FileInfo(audio).Length:0,AudioDurationSeconds=duration,ElapsedMonotonicMs=monotonic,ClockJumpSuspected=Math.Abs((ended-requested).TotalMilliseconds-monotonic)>2000,Error=error,Sources=sourceStatistics});
  if(!EvidenceJournal.Verify(journal.FilePath))throw new InvalidDataException("Falha na verificação da cadeia de eventos.");
  object callJournal=null;if(context.ContainsKey("CallSessionId")){string copy=Path.Combine(folder,"chamada.jsonl");CallAudit.CopyJournal(Convert.ToString(context["CallSessionId"]),copy);if(File.Exists(copy))callJournal=new{File="chamada.jsonl",Sha256=EvidenceFiles.Hash(copy)};}
  var summary=new StringBuilder();summary.AppendLine("PHONEDECK — REGISTRO LOCAL DE GRAVAÇÃO");summary.AppendLine("Identificador: "+id);summary.AppendLine("Solicitado em: "+requested.ToString("o"));summary.AppendLine("Encerrado em: "+ended.ToString("o"));summary.AppendLine("Fuso: "+TimeZoneInfo.Local.Id);summary.AppendLine("Duração de áudio (segundos): "+duration.ToString("F3",CultureInfo.InvariantCulture));foreach(var field in new[]{"Direction","ContactName","NumberDialed","CommandRequestedUtc","Association","Reason"})if(context.ContainsKey(field))summary.AppendLine(field+": "+Convert.ToString(context[field]).Replace("\r"," ").Replace("\n"," "));summary.AppendLine("Identidade do interlocutor não confirmada pela rede.");summary.AppendLine("Fonte de horário: relógio local do Windows, sem carimbo de tempo externo.");summary.AppendLine("Captação: microfone + saída selecionada; outros sons do PC podem ser incluídos.");summary.AppendLine("Áudio: "+Path.GetFileName(audio));summary.AppendLine("SHA-256 do áudio: "+(audioHash??"indisponível"));summary.AppendLine("Estado: "+(String.IsNullOrEmpty(error)?"finalizado":"parcial / "+error));summary.AppendLine("Os hashes não impedem que alguém com controle dos arquivos os reescreva; preserve o pacote original.");File.WriteAllText(Path.Combine(folder,"resumo.txt"),summary.ToString(),new UTF8Encoding(false));
  EvidenceFiles.Json(Path.Combine(folder,"manifesto.json"),new Dictionary<string,object>{{"SchemaVersion",1},{"RecordingId",id},{"ApplicationVersion",typeof(ConversationRecorder).Assembly.GetName().Version.ToString()},{"RequestedAt",requested.ToString("o")},{"EndedAt",ended.ToString("o")},{"ClockSynchronizationCertified",false},{"Context",context},{"Sources",sourceStatistics},{"Status",String.IsNullOrEmpty(error)?"finalized":"partial"},{"Error",error},{"Audio",new{File=Path.GetFileName(audio),Sha256=audioHash,Exists=exists}},{"Events",new{File="eventos.jsonl",Sha256=EvidenceFiles.Hash(journal.FilePath)}},{"Summary",new{File="resumo.txt",Sha256=EvidenceFiles.Hash(Path.Combine(folder,"resumo.txt"))}},{"CallJournal",callJournal}});
  File.WriteAllText(Path.Combine(folder,"manifesto.sha256"),EvidenceFiles.Hash(Path.Combine(folder,"manifesto.json"))+Environment.NewLine,new UTF8Encoding(false));
 }
}
