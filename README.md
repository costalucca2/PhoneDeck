# PhoneDeck

**Prévia experimental 1.16.4.62 — voz configurável e gravação manual; conversa real ainda não validada.**

Janela de contatos e controle Bluetooth HFP direto, com os mesmos comandos em Ulanzi, Stream Deck ou teclado macro. O caminho de chamadas não abre nem automatiza o Vincular ao Celular. Android ainda não foi validado em aparelho real. [Instalação e atalhos](docs/INSTALACAO.md).

**Experimental: controle e áudio são camadas diferentes.** Os três comandos foram validados com telefone simulado no binário entregue. Não foi feita chamada real. O backend usa o serviço HFP nativo com identidade própria MSIX. A conexão e a preparação da rota de voz foram aceitas em Windows 10 22H2, sem Phone Link e com zero chamadas. Isso ainda não comprova voz de ponta a ponta. A interface interna pode variar entre versões do Windows.

## Voz, tema e gravação

Configurações → Voz e Áudio: dispositivos, nível, mudo sincronizado, teste de sinal e redução de ruído opcional com RTX Voice. Sol/lua e PT/EN persistem entre aberturas. O minipainel usa verde para gravar e vermelho para parar/salvar microfone + saída em WAV local. Nenhuma gravação começa automaticamente. [Funcionamento e limites](docs/VOZ-E-GRAVACAO.md).

## Uso

Windows 10/11 x64 com .NET Framework 4.x, Bluetooth Classic e celular pareado com serviço Hands-Free Audio Gateway. Abra o PhoneDeck com o telefone ocioso e use Atualizar para conectar o controle. Um processo local sem console mantém a sessão HFP aberta entre os botões. Fechar a janela não encerra esse processo nem a chamada.

Se já houver chamada ativa/recebida e a sessão própria ainda não estiver conectada, PhoneDeck recusa reiniciar o HFP para não interferir. A conexão direta também pode disputar o perfil com outro aplicativo de chamadas; a instalação não desconecta programas ou chamadas existentes. Pareamento prévio não garante disponibilidade do canal. Havendo mais de um gateway HFP pareado, o programa recusa escolher um arbitrariamente.

```text
PhoneDeck.exe --ui
PhoneDeck.exe --contato mesa
PhoneDeck.exe --number NUMERO
PhoneDeck.exe --atender
PhoneDeck.exe --desligar
PhoneDeck.exe --snapshot
PhoneDeck.exe --check --contato mesa
PhoneDeck.exe --inspect --contato mesa
PhoneDeck.exe --answer-check
PhoneDeck.exe --audio-settings
PhoneDeck.exe --record-panel
PhoneDeck.exe --verify-recording "CAMINHO_DA_PASTA_DA_GRAVACAO"
PhoneDeck.exe --native-audio-setup
PhoneDeck.exe --native-stop
```

Ligar, Atender e Desligar usam a interface HFP nativa uma vez por solicitação. Ligar exige telefone ocioso; Atender exige uma única chamada recebida; Desligar exige uma única chamada. Ligar/Atender também solicitam a rota de áudio local; uma falha posterior de áudio informa que a chamada já foi comandada e não repete a discagem. Não há repetição automática nem fallback para janela/URI do Windows. Confirmação do comando pelo telefone não comprova conversa conectada.

As consultas snapshot/check/inspect/answer-check não enviam comandos de discagem, atendimento ou encerramento. Elas podem iniciar a conexão de controle quando o telefone estiver ocioso. Atualizar não lê mais a interface nem o histórico do Phone Link; o histórico do celular está indisponível no modo direto.

## Botões e contatos

Use ação Abrir com atalhos Windows para o executável e argumentos acima. Não há plugin oficial próprio de Stream Deck. Os atalhos existentes da Ulanzi continuam válidos após atualizar o executável: contatos para ligar, penúltimo botão Atender e último Desligar. Layout, relógio, ícones, pastas e dados privados são preservados pelo instalador.

Ao lado do executável, mantenha telefones/<id>.txt com um número incluindo DDD/código de operadora quando necessário. IDs: letras minúsculas de 2 a 24 caracteres. Nomes opcionais ficam em telefones-contatos.json. Esses dados, favoritos, capturas e logs ficam fora do Git.

## Áudio e Discord

--microfone alterna o mudo dos microfones padrão do Windows; --microfone-on e --microfone-off definem o estado. A interface mostra vermelho no mudo e verde ligado. Isso não comprova que a voz esteja roteada ao telefone. --audio-configure configura dispositivos padrão de comunicação e salva os IDs anteriores; só executar quando solicitado. Volume geral usa Core Audio.

--discord-microfone continua separado: restaura o Discord e envia Ctrl+Shift+M após verificar seu processo. O envio desse atalho não confirma o estado final do Discord.

## Compilar, testar e instalar

Siga a política de versões indicada em AGENTS.md antes de compilar.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\build.ps1
powershell -STA -NoProfile -ExecutionPolicy Bypass -File scripts\verify.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\install.ps1
```

build.json registra o hash do artefato e de todas as fontes. Test-HfpProtocol usa as classes do executável construído com um transporte simulado, sem acessar Bluetooth. Test-Window verifica a janela com contatos fictícios. verify.ps1 gera o registro necessário para instalar. A instalação padrão fica em %LOCALAPPDATA%/PhoneDeck; -Runtime permite atualizar outro diretório. A CI valida e publica uma prévia experimental. Para instalar, scripts/install.ps1 exige o registro de testes do artefato exato, serializa a instalação e preserva somente o backup imediatamente anterior depois de verificar os dois aliases. Configurações e contatos não são substituídos.

## Limites

Não há chamadas do chip por Wi-Fi, VoIP, SMS, importação nativa do histórico, bateria, DTMF ou áudio SCO próprio. As APIs públicas de controle completo PhoneCall/CallsPhoneContract 6 não estão presentes no Windows 10 testado. O diagnóstico separado NativeBridge 0.1.0.37 comprovou permissão, registro e conexão do transporte Windows; esse resultado não valida áudio nem os comandos do backend direto instalado.

O assistente QR abre somente um guia local temporário de pareamento, sem domínio nem app no celular; não constitui um protocolo de pareamento por QR. Não recebe contatos ou comandos de chamada.

O programa não inspeciona nem configura plataformas financeiras, não opera ordens e não depende de IA em runtime. O canal de comandos entre processos fica limitado ao usuário Windows atual. Chamadas não são repetidas após timeout.

## Licença

MIT: uso comercial e redistribuição permitidos com o aviso de licença. PhoneDeck não é afiliado à Microsoft, Apple, Elgato ou Ulanzi. QRCoder 1.8.1 (MIT), baixado com hash fixo, é embutido para o guia local. Não comercializar funções de voz ainda não entregues como disponíveis.
Use --native-stop para liberar o servidor próprio enquanto o telefone estiver ocioso. O comando não encerra chamadas, recusa parar durante uma chamada e não inicia um servidor ausente.
