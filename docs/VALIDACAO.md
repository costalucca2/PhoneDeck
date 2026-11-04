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

