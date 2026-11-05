# Validação da primeira versão

- Compilação x64 com manifesto PerMonitorV2: passou.
- Clique nativo em janela de laboratório isolada: passou, exatamente uma ativação.
- Número diferente do esperado: bloqueado no laboratório.
- Consulta de suporte a URI para seis contatos locais: passou, sem chamadas.
- Leitura real do estado do microfone pelo Core Audio: passou.
- Alinhamento dos dispositivos de comunicação aos dispositivos de mídia: executado e conferido por leitura independente.
- Ligação real, encerramento e mudo exclusivo do Discord: ainda não confirmados.
- Wi-Fi/rede para chamadas do chip: não implementado.

Falhas anteriores: InvokePattern retornava sem confirmação da discagem; GetClickablePoint falhava; a árvore de hit-test do Phone Link devolvia um painel genérico. A implementação usa limites visíveis do controle, contexto DPI e verificação de processo da janela sob o alvo. A solicitação de clique continua sem comprovar conexão telefônica.

Não há dependência de IA em execução, plugin de fabricante ou serviço remoto. Integração inicial por atalhos executáveis.

## 1.4.1.9 — latência

Avisos de áudio passaram a executar fora do processo da ação. A ação de configuração de áudio terminou em 320 ms neste computador, sem esperar os 4,5 segundos do aviso. O sucesso do atalho de mudo do Discord não abre aviso. A latência real do mudo do Discord ainda não foi medida.

Ícones vermelho/verde disponíveis em assets. Integração opcional de estado real via Discord Enhanced depende de aplicativo Discord com RPC e autorização; não está ativa nesta entrega. O pacote de terceiro não integra este repositório e mantém sua licença própria.


## 1.5.0.10 — Atender

Oito verificações dos nomes de botões passaram, incluindo rejeição de Ligar, Desligar e Recusar. A leitura das janelas reais não encontrou chamada recebida e não enviou ação. Atendimento real ainda não validado; o comando atua somente quando o Phone Link exibe botão de atendimento visível e habilitado.


## 1.6.0.12 — janela desktop

Janela instalada aberta e conferida visualmente: seis contatos locais visíveis, Ligar/Atender/Desligar e controle de microfone. Verificação independente de acessibilidade confirmou os cinco controles/indicadores esperados e janela respondendo. O microfone real foi lido como silenciado e exibido em vermelho. Nenhuma chamada foi iniciada, atendida ou encerrada na verificação; o mudo foi preservado. Capturas e contatos permanecem fora do repositório.


## 1.7.0.16 — interface e comparação com Phone Link

| Recurso | Verificação |
|---|---|
| Barra escura, botões e layout | Captura da janela revisada; seis contatos e discador visíveis sem cortes |
| Dispositivo e conexão | Leitura real do Phone Link, sem presumir conexão pelo processo |
| Chamadas recentes | 16 registros reais lidos da área Chamadas; apenas cache local |
| Busca por nome e telefone | Passou em fixture isolada |
| Favoritos | Filtro e gravação/leitura da preferência passaram em fixture isolada |
| Discador e apagar | Passou em fixture isolada; suporte ao URI consultado sem discar |
| Microfone vermelho/verde | Dois estados visuais testados sem alterar hardware; estado real lido como silenciado |
| Volume geral | Leitura real exibida; nenhum ajuste de volume efetuado no teste |
| Ulanzi | Seis atalhos de contatos consultados sem chamadas; destinos/argumentos de Atender, Desligar e áudio conferidos |
| Ligar, Atender e Desligar reais | Não executados: validação sem chamadas escolhida pelo usuário |

A instalação da janela e do auxiliar da Ulanzi foi conferida por hash do mesmo artefato. Ícone sincronizado do Discord permanece separado e pendente da integração autorizada. Não foi alterado nenhum programa de trading. Capturas, histórico, contatos e números não foram publicados.

## 1.7.1.18 — pintura real da janela

Corrigida sobreposição da legenda nativa do Windows: remoção explícita de WS_CAPTION, tratamento de WM_NCCALCSIZE/WM_NCPAINT/WM_NCACTIVATE. Botões limpam toda a superfície antes do desenho arredondado; painéis e filhos são invalidados após redimensionar.

Verificação em captura real do desktop, não apenas DrawToBitmap: sem barra branca e sem resíduos coloridos nos cantos. Repetido ciclo real de maximizar, minimizar, restaurar e redimensionar; a flag de legenda nativa permaneceu ausente. Reexecutados testes isolados da interface. Sem chamadas e sem alterações de áudio. Instalação e auxiliar Ulanzi conferidos por hash do mesmo artefato.

Para futuras correções de desenho, conferir captura da tela real após alternar foco e tamanho; a captura interna anterior não reproduzia o defeito do compositor/nonclient.

## 1.8.0.21 — apresentação e atalhos

- GUI e testes de seleção do fluxo de apresentação passaram.
- Retorno ao PhoneDeck/minimização da ponte exercitados com --presentation-test, sem chamadas.
- Seis atalhos reais de contato da Ulanzi consultados com --check: suporte disponível, sem discagem.
- Todos os atalhos da pasta apontam para PhoneDeck.exe. Últimos três comandos: Atender, Desligar, Microfone.
- Atalho Som Windows substituído por Abrir PhoneDeck; abrir a GUI existente não criou outra janela.
- Snapshot com a ponte minimizada ainda leu conexão e 16 registros reais.
- Ligar/atender/desligar em chamada real não foram executados, conforme escolha do usuário.

A ponte Phone Link é minimizada após os comandos; pode ficar visível durante o acionamento de sua interface. Não foi removida ou desinstalada.

## 1.9.0.22 — assistente QR local

- Testes de interface anteriores passaram novamente.
- Biblioteca QRCoder embutida carregada sem DLL instalada separadamente.
- Página em loopback: HTTP 200, instruções de iPhone e Android presentes; token incorreto retornou 404; POST retornou 405; Cache-Control no-store.
- Página no IP privado do próprio PC respondeu HTTP 200. Isso não comprova acesso a partir de um celular; depende do Firewall e isolamento da rede.
- QR extraído da captura real foi decodificado por OpenCV e correspondeu exatamente à URL temporária.
- QR expira em 10 minutos e a página encerra quando o assistente fecha.
- Nenhum contato, senha ou rota de chamada é servido. Não houve chamadas nem alterações no mudo ou no volume.
- Registro nativo: pacote de diagnóstico assinado registrado, mas a ativação não completou. Controle Bluetooth independente de Phone Link ainda não implementado/validado.

Capturas e URLs temporárias permanecem locais e fora do Git. Regra opcional de Firewall é específica para PhoneDeck.exe/TCP 40521/Private/LocalSubnet; não libera redes públicas.

Na instalação local de verificação, a confirmação administrativa da regra de Firewall foi cancelada e a regra não foi criada. Acesso HTTP pelo próprio PC e leitura do QR passaram; acesso a partir de outro aparelho permanece pendente. Não anunciar o acesso pela LAN como validado.

## Configuração local posterior à versão 1.9.1.25

Ulanzi: + em 4_1; engrenagem em 0_2; Atender em 1_2; Desligar em 2_2. O relógio usa os espaços seguintes. A engrenagem contém Voltar, Microfone e PhoneDeck. A pasta + é reservada para contatos extras. Destinos e argumentos conferidos sem executar chamadas. Firewall específico do guia local foi aplicado após nova autorização administrativa; o acesso desde um celular ainda não foi comprovado. Diagnóstico Bluetooth nativo ativou, mas acesso ao transporte foi negado; backend de chamadas instalado permanece Phone Link.

## 1.10.1.29 — prévia pública experimental
Testes sem aparelho: interface com contatos fictícios, protocolo HFP e dispatcher do binário exato, leitura de disponibilidade, guia QR local, compilação dos 11 diagnósticos sem execução, instalação limpa, atualização preservando contato fictício, backup único e reinstalação idempotente. Todos passaram localmente. Os testes não fizeram chamadas, não capturaram/reproduziram áudio e não registraram drivers/Appx. A CI Windows repete a verificação; seu resultado deve ser consultado no Actions do commit publicado. Áudio de chamadas no PC continua não implementado; a prévia não comprova telefonia completa.
