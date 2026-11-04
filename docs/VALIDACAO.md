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
