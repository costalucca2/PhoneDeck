# Instalação experimental

A preparação de voz pelo backend HFP nativo foi integrada e aceita pelo Windows em aparelho ocioso; **conversa real ainda não foi validada**. Os comandos de controle foram testados por simulação; a conexão HFP foi observada em um iPhone. Não há teste real de discar/atender/desligar nem validação em Android. Não usar esta prévia como único canal de atendimento operacional.

## Instalar a partir do código

Requisitos: Windows 10/11 x64 e .NET Framework 4.x. Compile usando o Windows PowerShell 5.1; a janela não exige Visual Studio ou driver próprio. O backend nativo exige também o pacote MSIX próprio descrito abaixo.

```powershell
git clone https://github.com/costalucca2/PhoneDeck.git
cd PhoneDeck
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\build.ps1
powershell -STA -NoProfile -ExecutionPolicy Bypass -File scripts\verify.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\install.ps1
& "$env:LOCALAPPDATA\PhoneDeck\PhoneDeck.exe" --ui
```

O build baixa QRCoder 1.8.1 com SHA-256 fixo. A verificação usa contatos e telefone simulados e não acessa Bluetooth, microfone ou plataformas financeiras. O instalador não registra os pacotes diagnósticos nem altera drivers, Firewall ou dispositivos padrão de áudio.

Contatos: use o + da janela, ou crie telefones/<id>.txt com um número próprio. O número não vai para o Git. Não existem contatos, senhas, certificados ou configurações pessoais incluídos.

## Atualizar uma instalação existente

Depois de compilar e verificar o novo artefato, execute:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\install.ps1 -Runtime "C:\caminho\da\instalacao"
```

Se houver servidor em segundo plano, encerre-o com PhoneDeck.exe --native-stop enquanto o telefone estiver ocioso. O comando não encerra chamadas. O instalador preserva contatos/configurações e guarda os executáveis e recibo anteriores em ligar-terra-anterior. Não há limpeza de dados pessoais.

## Ulanzi, Stream Deck e teclado macro

Crie atalhos Windows para PhoneDeck.exe, com o diretório de instalação como pasta de trabalho. Use a ação Abrir do programa do teclado:

| Botão | Argumentos |
|---|---|
| Abrir PhoneDeck | --ui |
| Ligar para contato cadastrado localmente | --contato exemplo |
| Atender | --atender |
| Desligar chamada única | --desligar |
| Alternar mudo do microfone Windows | --microfone |

Abrir a janela não faz ligação. Pressionar um botão de chamada é uma ação real, não um teste de disponibilidade. O mudo do Windows não comprova encaminhamento de voz ao iPhone. Perfis pessoais e senhas de teclados não são distribuídos.

## QR e diagnóstico

O QR abre um guia local temporário. Não concede acesso ao áudio da ligação celular pelo navegador. A conexão do chip continua dependendo de Bluetooth/HFP e do serviço HFP nativo; o QR não transporta áudio celular.

Os diagnósticos em tools/native-probe são ferramentas de desenvolvimento separadas. Os testes automatizados apenas os compilam. Não execute testes de streaming, registro de telefonia ou habilitação de perfil durante chamadas. Nenhum teste autorizado neste projeto deve fazer chamada real sem uma autorização específica.

## Identidade nativa de telefonia

O executável desktop sozinho recebe DeniedBySystem neste Windows. O servidor de telefonia roda no pacote próprio PhoneDeck.NativeBridge, ativado silenciosamente pelo cliente. Não utiliza identidade, token ou janela da Microsoft.

Após build e verify, use Windows PowerShell 5.1 e um certificado de assinatura CN=PhoneDeck já confiado para pacotes neste PC (chave privada somente no repositório local de certificados):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\install-native.ps1 -SdkTools "C:\caminho\Windows SDK\bin\x64" -CertificateThumbprint THUMBPRINT_DO_CERTIFICADO
& "$env:LOCALAPPDATA\PhoneDeck\PhoneDeck.exe" --native-audio-setup
```

SdkTools precisa conter makeappx.exe e signtool.exe assinados pela Microsoft. O script não instala certificados, não enfraquece segurança do Windows e não inclui chave privada no pacote. Se ainda não houver certificado confiável, provisioná-lo explicitamente antes da instalação; a distribuição assinada para usuários finais ainda não está automatizada.

A preparação exige celular ocioso e não disca, atende ou encerra chamada. Usa os dispositivos padrão de comunicação do Windows; não grava microfone durante a verificação. Resultado AudioPrepared significa solicitação aceita, enquanto AudioValidated continua false até validação autorizada em conversa. Não há fallback para Phone Link. O backend usa ABI interna observada em Windows 10 22H2; outras versões ainda precisam de homologação.