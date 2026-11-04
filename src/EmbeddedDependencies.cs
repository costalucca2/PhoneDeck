using System;using System.IO;using System.Reflection;
static class EmbeddedDependencies {
 static bool ready;
 public static void Load(){if(ready)return;ready=true;AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=>{if(new AssemblyName(e.Name).Name!="QRCoder")return null;using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("PhoneDeck.QRCoder.dll")){if(stream==null)return null;using(var memory=new MemoryStream()){stream.CopyTo(memory);return Assembly.Load(memory.ToArray());}}};}
}
