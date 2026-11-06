# Voz, áudio e gravação

Configurações → Voz e Áudio reúne microfone, saída, nível, mudo, medidor e redução de ruído. Os campos seguem o padrão visual de seleção do TradeON, com desenho próprio de fundo, seta e lista. Sol/lua alternam claro/escuro; US EN / BR PT alternam o idioma. A preferência fica em preferences.local.json, com as chaves on-tema e on-idioma. O mecanismo da família ON foi adaptado ao WinForms; os SVGs originais de sol/lua do NucleON são incorporados ao executável e renderizados como vetores. Cabeçalho compacto, controles sem moldura pesada e janelas separadas sincronizam preferências pelo arquivo local. Dados digitados e dispositivos selecionados são preservados ao alternar.

## Entrada e processamento

Os dispositivos selecionados são os padrões de comunicação do Windows. Isso também pode afetar outros programas que usam esse padrão. O nível modifica o microfone selecionado e não ultrapassa o limite do dispositivo. Ganho digital adicional acima de 100%, ganho automático e cancelamento de eco próprio ainda não estão implementados no transporte da chamada.

O mudo é sincronizado com SetMicMuteAsync do transporte HFP: o estado Core Audio sozinho não garantia que o microfone interno da chamada estivesse liberado. A preparação da voz também aplica essa sincronização. Uma falha é informada e não repete comandos de chamada.

“Verificar microfone” lê o sinal durante seis segundos, sem salvar ou reproduzir áudio. Sinal de entrada não comprova que o interlocutor recebeu a voz. O teste é iniciado pelo usuário e pode ser interrompido.

A organização se inspira nos controles documentados do [Discord](https://support.discord.com/hc/en-us/articles/33030151293079-Discord-Voice-Video-Streaming-Guide). O [guia técnico do Discord](https://github.com/discord/discord-api-docs/blob/main/developers/discord-social-sdk/development-guides/managing-voice-chat.mdx) distingue supressão WebRTC, cancelamento de eco, ganho e Krisp. Esses componentes proprietários não foram copiados para o PhoneDeck.

## Redução de ruído opcional

Em NVIDIA GTX/RTX compatível, o [RTX Voice oficial](https://www.nvidia.com/en-us/geforce/guides/nvidia-rtx-voice-setup-guide/) fornece um microfone virtual processado. O instalador e os modelos não são distribuídos neste repositório. Instalação e permissões do componente são separadas do pacote PhoneDeck.

Com RTX Voice instalado, selecione a origem física e a intensidade no PhoneDeck. Ativar a redução escolhe o microfone virtual para comunicação; desativar escolhe a origem física. Alterar o processador exige celular ocioso e nenhuma gravação ativa, porque reinicia o aplicativo de processamento. A execução usa a pasta do RTX Voice como diretório de trabalho para localizar o modelo de ruído.

A configuração observada usa microfone físico → RTX Voice → entrada de comunicação. A saída continua na caixinha física, com processamento de alto-falante desligado. Não há garantia de funcionamento em GPU sem suporte, nem paridade com todos os efeitos do Discord.

## Gravar conversa

“Gravar conversa” abre um minipainel. Verde inicia; vermelho para e salva. O painel informa o tempo e permite abrir a pasta. Quando encontra o cartão Chamada do Windows, posiciona-se abaixo dele, sem modificar seu processo. Arrastar o painel fixa a posição escolhida. Uma chamada comandada pelo PhoneDeck ou Ulanzi pode abrir o painel, mas nunca inicia gravação automaticamente.

O gravador usa WASAPI compartilhado: microfone de comunicação e loopback da saída de comunicação selecionada. Portanto, outros sons reproduzidos nessa saída também podem entrar no arquivo. O sinal do microfone filtrado é gravado quando a entrada selecionada é RTX Voice. Não são acionados comandos de telefone para gravar.

Destino padrão: Desktop/Gravacoes/PhoneDeck. Arquivos WAV PCM, 48 kHz, mono, 16 bits, aproximadamente 5,8 MB/minuto. O cabeçalho é atualizado periodicamente e no encerramento. O limite de WAV de 4 GiB encerra e salva a gravação; falhas de captura aparecem no painel. Não existe pausa, gravação oculta, início automático ou envio externo de áudio.

Uma trava impede dois gravadores simultâneos. Fechar o painel durante a gravação salva o arquivo. O instalador recusa atualizar um processo com gravação ativa e não apaga gravações. --record-panel abre o painel sem começar a capturar.

## Evidência e limites

Os testes usam sinais sintéticos para verificar mistura das duas fontes, WAV, salvamento, estados verde/vermelho, temporizador, exclusão mútua e falhas. Preferências e campos foram conferidos em claro/escuro e PT/EN. No ambiente local, os dois streams reais foram inicializados e imediatamente parados antes de ler amostras, sem arquivo de áudio nem chamada.

Acesso HFP, rota local e sincronização de mudo foram aceitos pelo Windows. Isso não comprova conversa bidirecional nem gravação do interlocutor em chamada real. Não foram realizadas chamadas de teste. O painel “Cha” corresponde ao título cortado “Chamada” do componente CallingShellApp do Windows, identificado pelos recursos locais; não é o nome do PhoneDeck.

## Registro associado à gravação

Cada nova gravação cria sua própria pasta em Desktop/Gravacoes/PhoneDeck, com conversa.wav original, eventos.jsonl, resumo.txt, manifesto.json e manifesto.sha256. Se existe contexto recente de uma chamada comandada no PhoneDeck, inclui também uma cópia chamada.jsonl. Gravações anteriores não são alteradas ou preenchidas retrospectivamente.

O registro contém início/fim com UTC, horário local e fuso, duração monotônica, versão, entrada/saída, mudo e nível no início, falhas de captura e estados HFP observados a cada cinco segundos quando mudam. Os contadores de amostras e preenchimento com silêncio não medem perdas da rede. O áudio inclui o microfone e tudo que tocar na saída selecionada, sem isolar o interlocutor.

Nos comandos do PhoneDeck/Ulanzi, o log distingue solicitação, confirmação do comando e observação de estado. Guarda o nome local e o número solicitado com seus zeros/prefixos, não uma identidade confirmada pela operadora. A associação da gravação usa contexto local recente e uma única chamada observada; não existe identificador de chamada da rede. Sem essa associação, os dados ficam desconhecidos. Chamadas recebidas não têm número remoto validado. O log de comandos em chamadas/<id>/eventos.jsonl pode continuar após o encerramento da gravação; a cópia do pacote corresponde ao momento do salvamento.

Os eventos formam uma cadeia SHA-256; o manifesto vincula áudio, eventos e resumo. Para conferir sem telefone: PhoneDeck.exe --verify-recording "PASTA_DA_GRAVACAO". O processo retorna 0 se os arquivos correspondem e 1 se há inconsistência ou pacote incompleto; salva verificacao.local.txt. Um pacote parcial pode ter integridade válida, mas o manifesto e o resumo indicam a falha. Falha de energia antes do salvamento final pode deixar WAV/logs recuperáveis e pacote incompleto.

Preserve a pasta inteira e uma cópia independente do manifesto.sha256. Os hashes detectam alterações em relação à cópia preservada; quem pode reescrever todos os arquivos pode recalculá-los. O relógio é o do Windows, sem carimbo de tempo externo certificado. O pacote não certifica autenticidade, identidade, causa de falha de plataforma ou aceitação jurídica. Não há upload, gravação automática ou mudança de plataformas financeiras.

## Histórico local na janela

Histórico do PhoneDeck mostra os comandos registrados em Desktop/Gravacoes/PhoneDeck/chamadas: contato, número solicitado, data/hora local, ação e resultado. Atualiza em segundo plano a cada cinco segundos e pelo botão Atualizar, mesmo sem acesso ao histórico do celular. Comando aceito não significa conversa conectada. Mostra tentativas sem confirmação e falhas; registros alterados/ilegíveis produzem aviso. Até 200 registros recentes são exibidos. Não recupera chamadas antigas sem logs nem chamadas feitas exclusivamente em outro aplicativo. A lista é somente leitura e não dispara chamadas ao selecionar uma linha.

Clique em uma linha do histórico (ou Enter na linha selecionada) para abrir o registro no PhoneDeck: contato, número solicitado, horário local com fuso, UTC, log da chamada e gravações associadas pelo CallSessionId. Se houver mais de uma gravação na mesma sessão, escolha na lista. Ouvir gravação reproduz o WAV no próprio programa; Parar áudio interrompe, e fechar a janela também para. Não inicia reprodução sozinho. Abrir pasta mostra os originais. Sem gravação associada, o log continua acessível e a ausência é informada. Não associa por número ou proximidade de horário. Gravações parciais são identificadas; falha de integridade bloqueia reprodução. Os arquivos originais não são modificados.

O minipainel de gravação acompanha a presença de um cartão visível do CallingShellApp: sem cartão, fica oculto, inclusive quando aberto manualmente. Ao desaparecer o cartão, uma gravação ativa é salva com o motivo call_card_closed. Quando o cartão reaparece, o painel volta sem iniciar captura automaticamente. Arrastar muda apenas a posição; a regra de visibilidade continua. O cartão do Windows é apenas observado, sem receber cliques ou comandos.

## Estado visível de gravação

O botão do painel mostra REC com cronômetro quando a captura iniciou, além de Iniciando/Salvando e falhas. O cabeçalho do PhoneDeck acompanha a gravação entre processos por recording-state.local.json, com atualização periódica, identidade do processo e indicação de estado desconhecido quando não é possível confirmar.

A integração Ulanzi está em integrations/ulanzi: gravação de telas continua usando TradeRecorder.exe; gravação de conversa abre o painel manual do PhoneDeck. Cada ação mostra seu próprio estado, sem confundir áudio com vídeo. O plugin envia imagem REC vermelha e tempo decorrido; não inicia captura ao carregar, adicionar botão ou atualizar imagem. Apenas o evento run de um clique confirmado dispara a ação. O aplicativo e o teclado não substituem uma confirmação de que o interlocutor recebeu a voz.
