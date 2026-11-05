using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PhoneDeckNative {
 public interface IAtChannel { string Execute(string command); }
 public sealed class DeadlineAtChannel:IAtChannel {
  readonly IAtChannel inner;readonly Func<DateTime> now;
  public DateTime DeadlineUtc;
  public DeadlineAtChannel(IAtChannel inner,DateTime deadlineUtc,Func<DateTime> now=null){if(inner==null)throw new ArgumentNullException("inner");this.inner=inner;DeadlineUtc=deadlineUtc;this.now=now??(()=>DateTime.UtcNow);}
  public string Execute(string command){if(now()>=DeadlineUtc)throw new TimeoutException("Prazo do comando expirou; nenhuma nova instrução será enviada ao telefone.");return inner.Execute(command);}
 }
 public sealed class HfpState {
  public int Call, Setup, Held;
  public bool Idle { get { return Call==0 && Setup==0 && Held==0; } }
 }
 public sealed class HfpProtocol {
  readonly IAtChannel channel;
  readonly List<string> indicators=new List<string>();
  readonly object gate=new object();
  bool ready;
  public HfpProtocol(IAtChannel channel){if(channel==null)throw new ArgumentNullException("channel");this.channel=channel;}
  public bool Ready {get{return ready;}}
  string Send(string command){string response=channel.Execute(command);if(response==null||!Regex.IsMatch(response,@"(?:^|[\r\n])OK(?:[\r\n]|$)"))throw new InvalidOperationException("O iPhone não confirmou o comando Bluetooth.");return response;}
  public HfpState Initialize(){lock(gate){ready=false;indicators.Clear();Send("AT+BRSF=32");string definition=Send("AT+CIND=?");foreach(Match m in Regex.Matches(definition,"\\(\"([a-z]+)\"\\s*,",RegexOptions.IgnoreCase))indicators.Add(m.Groups[1].Value.ToLowerInvariant());if(!indicators.Contains("call")||!indicators.Contains("callsetup"))throw new InvalidOperationException("O telefone não informou os indicadores obrigatórios de chamada.");HfpState state=ReadState();Send("AT+CMER=3,0,0,1");ready=true;return state;}}
  HfpState ReadState(){string response=Send("AT+CIND?");Match match=Regex.Match(response,@"\+CIND:\s*([0-9, ]+)");if(!match.Success)throw new InvalidOperationException("Estado Bluetooth ausente.");string[] values=match.Groups[1].Value.Trim().Split(',');if(values.Length!=indicators.Count)throw new InvalidOperationException("Indicadores Bluetooth incompletos.");var state=new HfpState();for(int i=0;i<values.Length;i++){int value;if(!Int32.TryParse(values[i].Trim(),out value))throw new InvalidOperationException("Indicador Bluetooth inválido.");if(indicators[i]=="call")state.Call=value;else if(indicators[i]=="callsetup")state.Setup=value;else if(indicators[i]=="callheld")state.Held=value;}if(state.Call<0||state.Call>1||state.Setup<0||state.Setup>3||state.Held<0||state.Held>2)throw new InvalidOperationException("Estado de chamada inválido.");return state;}
  void EnsureReady(){if(!ready)throw new InvalidOperationException("Conexão Bluetooth nativa ainda não está pronta.");}
  public HfpState Status(){lock(gate){EnsureReady();return ReadState();}}
  public void Dial(string number){if(number==null||!Regex.IsMatch(number,@"\A(?:\+[1-9][0-9]{7,14}|[0-9]{8,14})\z"))throw new ArgumentException("Número inválido.");lock(gate){EnsureReady();if(!ReadState().Idle)throw new InvalidOperationException("Já existe uma chamada no telefone.");Send("ATD"+number+";");}}
  public void Answer(){lock(gate){EnsureReady();HfpState state=ReadState();if(state.Setup!=1||state.Call!=0||state.Held!=0)throw new InvalidOperationException("Não há uma chamada recebida única para atender.");Send("ATA");}}
  public void Hangup(){lock(gate){EnsureReady();HfpState state=ReadState();if(state.Idle)throw new InvalidOperationException("Não há chamada para encerrar.");string calls=Send("AT+CLCC");if(Regex.Matches(calls,@"(?m)^\+CLCC:").Count!=1)throw new InvalidOperationException("Não foi identificada uma única chamada para encerrar.");Send("AT+CHUP");}}
 }
}
namespace PhoneDeckNative {
 public static class HfpSdp {
  public static int RfcommChannel(byte[] descriptor){if(descriptor==null)throw new ArgumentNullException("descriptor");int channel=0,count=0;for(int i=0;i+4<descriptor.Length;i++){if(descriptor[i]==0x19&&descriptor[i+1]==0&&descriptor[i+2]==3&&descriptor[i+3]==8){int value=descriptor[i+4];if(value<1||value>30)throw new ArgumentException("Canal RFCOMM inválido.");channel=value;count++;}}if(count!=1)throw new ArgumentException("Descritor RFCOMM ausente ou ambíguo.");return channel;}
 }
}
namespace PhoneDeckNative {
// The dispatch layer is independently tested with a simulated channel.
public sealed class HfpCommandDispatch {
 readonly HfpProtocol protocol;
 public HfpCommandDispatch(HfpProtocol protocol){this.protocol=protocol;}
 public HfpState Execute(string action,string number){
  switch(action){
   case "status":return protocol.Status();
   case "dial":protocol.Dial(number);return null;
   case "answer":protocol.Answer();return null;
   case "hangup":protocol.Hangup();return null;
   default:throw new ArgumentException("Comando PhoneDeck inválido.");
  }
 }
}


}
