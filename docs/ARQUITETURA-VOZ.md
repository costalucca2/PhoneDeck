# Engenharia reversa de interoperabilidade: voz e chamadas

Verificação em 05/11/2026, sem executar Phone Link nem fazer chamadas.

## Caminho encontrado no aplicativo instalado

Análise estática, sem copiar bibliotecas para o produto:

- HfpCallLine.DialAsync chama PhoneLine.Dial quando CanDial permite.
- A alternativa de interface própria usa PhoneLine.DialWithResultAsync/GetAllActivePhoneCallsAsync e objetos PhoneCall.
- O adaptador PhoneLine verifica CallsPhoneContract 6 antes dos métodos novos; sem esse contrato, recusa-os.
- O adaptador PhoneCall usa ChangeAudioDeviceAsync com LocalDevice/RemoteDevice para rotear a voz. O painel de áudio não implementa uma ponte equivalente a ler e escrever PCM arbitrariamente.
- Neste Windows 10, medições anteriores confirmaram CallsPhoneContract 5 presente, 6 ausente e tipo PhoneCall ausente. Não transportar um resultado de Windows 11 para essa máquina.

Isso reforça que a estratégia atual do PhoneDeck (sinalização RFCOMM própria + tentativa de pinos PCM separada) não reproduz o gerenciamento de telefonia/áudio do aplicativo de referência. Não há evidência suficiente de que Phone Link possa ser tratado como servidor invisível com API completa neste Windows. Ainda não foi encontrada integração própria completa, suportada e verificada que cumpra os três comandos e voz sem qualquer janela Windows nesta instalação.

A análise identifica interfaces e dependências; não autoriza reutilizar tokens, identidade de outro pacote ou código proprietário. Resultados privados de inspeção permanecem em out, fora do Git.

## Aplicativo iOS e QR de navegador

Link to Windows é opcional no iPhone segundo o suporte Microsoft. Chamadas iPhone/PC seguem pelo Bluetooth; instalar o app iOS não implementa o transporte SCO próprio ausente no PhoneDeck.

Uma página com getUserMedia/WebRTC pode captar o microfone autorizado e fazer uma chamada de internet com um par WebRTC. Essa API não expõe o áudio de entrada/saída da ligação celular do iPhone. A especificação descreve inclusive que uma ligação telefônica pode retirar o acesso ao microfone do navegador. Assim, QR/PWA não é substituto válido para atender a corretora usando o chip do iPhone no PC. O guia QR existente continua apenas guia, não pareamento ou áudio de chamadas.

Fontes primárias:
- https://support.microsoft.com/en-au/windows/apps/phonelink/phone-link-requirements-and-setup
- https://support.microsoft.com/en-au/windows/apps/make-and-receive-phone-calls-from-your-pc
- https://w3c.github.io/mediacapture-main/
- https://developer.apple.com/documentation/AVFAudio/handling-audio-interruptions
- https://learn.microsoft.com/en-us/windows-hardware/drivers/bluetooth/bluetooth-driver-stack

## Situação de entrega

PhoneDeck 1.10.0.28 permanece sem áudio de chamadas implementado. Diagnósticos de acesso/conexão não equivalem a fluxo de voz. Não foi instalado aplicativo no iPhone, criado WebRTC que simule telefonia celular, aberto o aplicativo Windows ou executada chamada. A integração completa permanece pendente; este documento é resultado de investigação, não comprovação de funcionamento.
## Atualização: backend nativo com identidade própria

A versão 1.11.0.31 substitui o socket RFCOMM separado por SystemHfp/SystemHfpDispatch. O pacote PhoneDeck.NativeBridge tem a mesma assembly testada da janela e executa o servidor em MTA, sem interface da Microsoft. HfpTransportDevice é ativado via IActivationFactory.ActivateInstance e consulta da interface statics, não por solicitação direta da factory statics.

IIDs e slots foram conferidos nos metadados COM e símbolos públicos da versão instalada de BTAGService.dll. SetAudioTransferAsync(true) chama ConnectAudioAsync; false chama DisconnectAudioAsync. Não copiar DLLs, símbolos ou código do sistema para a distribuição. A API é interna, portanto falha fechada se indisponível.

Evidência local: acesso Allowed, registro próprio, ConnectAsync true, lista de chamadas vazia e preparação de áudio com resultado 0. Entrada ME6S e saída Realtek são os dispositivos padrão de mídia e comunicação, microfone ligado. Não foram feitas chamadas nem captura de microfone. Isso prova preparação aceita, não áudio de conversa. O teste de pinos KS continua sem RUN durante repouso e não é usado como backend.

O broker usa novo pipe por usuário e ativa somente a identidade PhoneDeck. Comandos de chamada usam métodos nativos, guardas e prazo por solicitação; não repetem comandos após falha de áudio. Test-SystemHfp valida o mesmo dispatcher do binário com transporte simulado. Os resultados anteriores deste documento descrevem etapas históricas, não o backend atual.