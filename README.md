# PhoneDeck

Controle de chamadas do Vincular ao Celular por botões físicos: Stream Deck, Ulanzi e teclados macro que executem programas.

**Estado: experimental.** Compilação, detecção do discador e áudio podem ser verificados sem fazer chamadas. Ligação e encerramento reais ainda não foram validados nesta entrega. Não há plugin oficial próprio para Stream Deck: use a ação Abrir com atalhos Windows.

## Conexões

- iPhone e Android: chamadas do chip através de Bluetooth e Microsoft Phone Link configurado.
- Wi-Fi/rede: não implementado para chamadas do chip. Recursos adicionais do Phone Link dependem do aparelho.
- Não é telefonia VoIP e não substitui a linha do celular.

## Requisitos

Windows 10/11 x64, .NET Framework 4.x, Phone Link instalado e celular pareado. A interface precisa estar acessível na sessão Windows do usuário. Sem serviço remoto ou credenciais externas.

## Compilar

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\build.ps1
```

A saída fica em `out/PhoneDeck.exe`. O registro `out/build.json` inclui versão e hashes. O script usa o compilador .NET Framework do Windows, sem pacotes externos.

## Contatos privados

Ao lado do executável, crie `telefones/mesa.txt` com um único número, incluindo DDD e código de operadora quando necessários. Use IDs em letras minúsculas de 2 a 24 caracteres. Números e contatos ficam fora do Git.

```text
PhoneDeck.exe --contato mesa
PhoneDeck.exe --desligar
PhoneDeck.exe --microfone
PhoneDeck.exe --microfone-on
PhoneDeck.exe --microfone-off
PhoneDeck.exe --audio-configure
PhoneDeck.exe --audio-settings
PhoneDeck.exe --audio-status
PhoneDeck.exe --check --contato mesa
PhoneDeck.exe --inspect --contato mesa
```

- `--check`: verifica suporte ao URI, sem chamada; não comprova Bluetooth ou discagem.
- `--inspect`: lê o discador e compara o número; não disca.
- `--audio-status`: grava estado do microfone em arquivo local, sem alterar.
- `--microfone`: alterna mudo dos microfones padrão de mídia e comunicação. É mudo do Windows, não só desta chamada.
- `--audio-configure`: usa para chamadas os dispositivos padrão de reprodução e captura usados no áudio normal; salva os IDs anteriores em arquivo local. Não altera ganho nem mudo.
- `--audio-settings`: abre as configurações de som do Windows.
- `--desligar`: procura botão visível de encerrar em janelas do Phone Link; não envia atalhos globais a outras plataformas.

## Botões

Crie atalhos `.lnk` para o executável com os argumentos acima. Use ação Abrir da Ulanzi, Sistema > Abrir do Stream Deck ou o software do teclado. O executável funciona sem console PowerShell. Não contém integração HID, plugin Stream Deck nem perfis de fabricantes publicados ainda.

## Limites e segurança operacional

O Phone Link não fornece uma API pública de telefonia para este fluxo; usamos UI Automation e clique nativo limitado à janela identificada. A Microsoft pode mudar controles. O clique movimenta o cursor brevemente e o restaura. Se o Windows recusar foco, se outro programa cobrir o alvo ou se o número mudar, a operação é bloqueada. Não há repetição automática de chamada. Uma solicitação enviada não comprova conexão.

Não inspeciona nem configura plataformas financeiras. Não armazena contatos no repositório. O controle local não depende de IA em tempo de execução.

## Comercial

Licença MIT: uso comercial, modificação e redistribuição permitidos com manutenção do aviso de licença. Pode ser oferecido com serviço pago de instalação, configuração de teclados e suporte. PhoneDeck não é afiliado à Microsoft, Apple, Elgato ou Ulanzi.

## Referências examinadas

- [Heartran/phonelink-mcp-server](https://github.com/Heartran/phonelink-mcp-server): projeto público com comando de iniciar chamada; depende de UI e não oferece aqui o conjunto completo de áudio/encerramento. Código não incorporado.
- [Microsoft: configurar chamadas](https://support.microsoft.com/en-us/windows/apps/phonelink/setting-up-calls-in-the-phone-link).
- [Microsoft: UI Automation e escala](https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-screenscaling).

## Discord

Use `--discord-microfone` para alternar o mudo exclusivo do Discord desktop. Abre/restaura sua janela e envia Ctrl+Shift+M somente após confirmar o processo em primeiro plano. Não altera o mudo global. O comando enviado não confirma o estado final do Discord.


O controle de estado real com ícone vermelho/verde depende de integração autenticada com o Discord. Os ícones estão em assets; o atalho básico não comprova nem sincroniza o estado. A integração ainda está pendente. Avisos de áudio não bloqueiam mais o próximo comando.


## Atender ligações recebidas

`PhoneDeck.exe --atender` procura um botão Atender/Aceitar/Answer visível e habilitado apenas em janelas do processo PhoneExperienceHost. Não usa atalho global e não inicia ligação de saída. `--answer-check` verifica a disponibilidade desse controle e grava answer-status.local.txt sem atender. Atender precisa de chamada recebida exibida no Phone Link; notificações exclusivas da Central de Ações do Windows não são acionadas nesta versão.


## Janela própria

Abra `PhoneDeck.exe` sem argumentos (ou com `--ui`) para usar a janela de contatos. Os botões Ligar, Atender e Desligar executam os mesmos comandos dos teclados. O indicador e o botão de microfone mostram o estado real do Windows, vermelho no mudo e verde ligado, atualizado a cada segundo. O painel não confirma sozinho uma chamada conectada. Contatos vêm de `telefones/*.txt` e nomes opcionais de `telefones-contatos.json`; ambos devem permanecer privados. Abrir a janela não faz chamadas nem altera o mudo.


## Interface de terminal

Barra própria com arrastar, redimensionar, minimizar, maximizar e fechar. Contatos com pesquisa por nome/telefone, favoritos persistentes e cadastro local. Discador numérico com apagar. Microfone do Windows e volume geral lidos pelo Core Audio; o volume só muda por ação do usuário.

O botão Atualizar lê aparelho, conexão e até 16 registros da área Chamadas do Phone Link, quando sua janela estiver acessível. O histórico é uma leitura datada, não um monitor de ligação ativa. Cache e favoritos são arquivos locais ignorados pelo Git. Não há SMS, leitura de bateria nem controle de tons DTMF nesta versão.

`--snapshot` faz essa leitura sem chamadas. `--number NUMERO` disca um número validado usando a mesma proteção de alvo do comando de contatos; `--check --number NUMERO` apenas consulta suporte ao URI. A consulta não comprova que o aparelho disca.

Verificação local sem chamadas:

```powershell
powershell -STA -NoProfile -ExecutionPolicy Bypass -File tests\Test-Window.ps1
```

## PhoneDeck como janela principal

Os atalhos podem apontar diretamente para PhoneDeck.exe. Após ligar, atender ou desligar, o auxiliar minimiza as janelas visíveis do Phone Link e retorna ao PhoneDeck; o contato acionado pelo teclado é selecionado na janela. Abrir `--ui` novamente foca a janela existente.

O Microsoft Phone Link continua obrigatório como ponte Bluetooth. Sua janela pode aparecer brevemente para acionar os controles; esta integração não substitui a telefonia do Windows. Atender/Desligar restauram a ponte antes de procurar seus botões e devolvem o foco depois. Consultas `--check`, `--inspect`, `--answer-check` e `--snapshot` não mudam o foco por esse mecanismo. Microfone do Discord continua separado.
