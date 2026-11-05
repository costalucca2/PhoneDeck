using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;
static class PhoneSnapshot {
 public static string CallReadiness(bool selected,bool dialer,bool error){return !selected?"Unverified":error?"Error":dialer?"DialerAvailable":"Unavailable";}
 public static void Save(){
  var result=new Dictionary<string,object>{{"Updated",DateTime.Now.ToString("o")},{"Device","Celular Bluetooth"},{"Connection","Controle desconectado"},{"CallsState","Unavailable"},{"CallsError",""},{"DeviceConnection","Sem leitura"},{"Backend","HFP direto"},{"AudioValidated",false},{"Recent",new object[0]}};
  try{var state=NativeHfpBridge.Request("status");result["Device"]=state["Device"];result["DeviceConnection"]="Conectado";result["CallsState"]="ControlConnected";result["Connection"]="Controle conectado · áudio pendente";result["Call"]=state["Call"];result["Setup"]=state["Setup"];result["Held"]=state["Held"];result["CallsError"]="Controle HFP direto. Áudio no PC ainda não validado.";}catch(Exception e){result["CallsError"]=e.GetBaseException().Message;}
  var file=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"phone-snapshot.local.json");var temp=file+".tmp";File.WriteAllText(temp,new JavaScriptSerializer().Serialize(result));if(File.Exists(file))File.Delete(file);File.Move(temp,file);
 }
}