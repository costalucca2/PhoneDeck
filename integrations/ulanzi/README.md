# Ulanzi Recording

Integração própria conforme o protocolo local documentado no [SDK Ulanzi](https://github.com/UlanziTechnology/plugin-common-node). Não inclui código nem dependências de plugins terceiros.

1. Compile/verifique/instale PhoneDeck pelo fluxo do repositório, seguindo a política de versões.
2. Copie com.lucca.recording.ulanziPlugin para %APPDATA%/Ulanzi/UlanziDeck/Plugins/.
3. Crie nessa pasta config.local.json em UTF-8 sem BOM: {"phoneDeck":"C:/CAMINHO/PhoneDeck.exe"}. Não versionar caminhos pessoais/configuração local.
4. Reinicie somente a Ulanzi Studio quando nenhuma gravação estiver ativa. Adicione a ação desejada pela categoria PhoneDeck.

Gravar telas chama TradeRecorder.exe no mesmo diretório da instalação. Esse gravador é um componente local separado, não distribuído por esta integração. Gravar conversa abre o painel manual; a captura começa no botão Gravar do painel, somente com cartão de chamada visível.

A ponte é o mesmo executável com --ulanzi-bridge, sem console, aceita apenas localhost e se conecta à porta indicada pela Ulanzi. Não faz chamadas nem captura áudio por conta própria. Imagens são geradas no processo, com estado parado/iniciando/gravando/salvando/erro e cronômetro. Um estado desconhecido pede verificação; imagem vermelha não é usada como confirmação de voz remota.

Para telas, lê o estado publicado pelo gravador existente e confere presença do processo. Para conversas, lê estado atualizado pelo worker e confere identidade do processo e atualização recente. O instalador do PhoneDeck pode encerrar a ponte para atualizar o executável; reinicie a Ulanzi depois. Ele continua bloqueando atualização durante gravação de conversa.

Testes: Test-Recording/Test-RecordingUi usam captura sintética. Test-UlanziBridge verifica JavaScript e exercita registro/contexto/PNG com servidor WebSocket local sem enviar cliques de gravação.