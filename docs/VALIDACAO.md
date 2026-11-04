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
