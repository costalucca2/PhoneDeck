# Diagnósticos de desenvolvimento

Não fazem parte do aplicativo instalado. A CI apenas compila os fontes, sem executar testes Bluetooth, chamadas, streaming, mudanças de driver ou registros Appx.

- NativeProbe/NativeLines: enumeração de identidade e linhas.
- NativeAccess: consulta permissões, solicita acesso quando necessário, registra transporte quando Allowed e tenta ConnectAsync; não faz chamadas. Requer autorização para essas ações.
- HfpProbe/RawHfpProbe: negociam RFCOMM/HFP sem discar, atender, desligar ou transmitir voz; abortam diante de chamada ativa/recebida.
- TapiProbe: enumera provedores sem abrir linhas.
- ClassicBluetoothStatus: vínculo Classic e serviços habilitados, sem alterações.
- EnableIphoneHfp: habilita somente o perfil HFP do aparelho explicitamente informado; não é consulta somente leitura.
- AudioPinProbe: consulta formatos/interfaces dos pinos KS, somente GET.
- AudioStreamProbe: tenta abrir e ativar os dois pinos PCM sem amostras; aborta durante chamada. Código de saída 3 indica falha de RUN; saída 0 exige os dois canais ativados. Não comprova conversa ou áudio ponta a ponta.
- NativeAudioAccess: executa o teste de pinos com identidade própria. Para compilar com AudioStreamProbe, use /define:NATIVE_AUDIO_PACKAGE e /main:NativeAudioAccess, como em tests/Test-DiagnosticsBuild.ps1. Executar sem pacote apropriado não estabelece identidade. Certificados, pacotes e tokens pessoais não são distribuídos.

Consulte docs/BLUETOOTH-NATIVO.md e docs/ARQUITETURA-VOZ.md. Acesso Allowed, linha CanDial, registro e handshake não equivalem a áudio funcional. O aplicativo experimental usa sinalização HFP direta; áudio SCO continua pendente. Repositório e testes não incluem contatos reais.