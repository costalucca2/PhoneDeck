# Controle Bluetooth nativo — diagnóstico

Objetivo solicitado: discar, atender e desligar no PhoneDeck/Ulanzi sem exibir a interface Microsoft Phone Link. A automação de UI atual não cumpre esse requisito; minimizar depois não equivale a um backend nativo.

Inspeção estritamente de leitura executada:

- GetCurrentPackageFullName: 15700 (APPMODEL_ERROR_NO_PACKAGE), sem identidade de pacote.
- PhoneLineTransportDevice.GetDeviceSelector + DeviceInformation.FindAllAsync: um transporte HFP encontrado.
- IsRegistered: false para o executável de diagnóstico.
- Não foram chamadas RequestAccessAsync, RegisterApp, Connect, Dial, Answer ou Hangup.

Isso comprova detecção do transporte pelo Windows, não acesso autorizado, áudio nem controle de chamadas. O diagnóstico não fez nem recebeu chamadas.

Próxima etapa: preparar identidade de pacote apropriada e testar acesso explícito ao transporte de telefonia. A execução que incluía RequestAccessAsync foi rejeitada pela revisão automática porque poderia solicitar/conceder acesso; a variante corrigida de somente leitura passou. Não usar essa rejeição como prova de que o Windows negou acesso: a solicitação não chegou a executar.

A substituição do backend ainda não está implementada/validada. Não declarar que a versão instalada funciona sem Phone Link ou que a API nativa foi liberada.

Referências:

- https://learn.microsoft.com/en-us/uwp/api/windows.applicationmodel.calls.phonelinetransportdevice
- https://github.com/BestOwl/MyPhone (implementação Windows com licença MIT, candidata a estudo)
- https://github.com/gutbash/blue (implementação Linux; não é substituto direto no Windows)

## Registro do diagnóstico nativo

Foi criado um certificado de assinatura PhoneDeck com chave não exportável no armazenamento pessoal e certificado público em TrustedPeople; não foi adicionado a Trusted Root. O pacote PhoneDeck.NativeProbe 0.1.0.6 foi assinado e registrado para o usuário atual. O modo de desenvolvedor e as políticas globais de sideload não foram habilitados.

O pacote leve foi aceito, mas o processo direto continuou sem identidade (15700). Foi testado também pacote completo. A ativação oficial retornou 0x80270254 e não chegou ao pedido de acesso. A chamada Invoke-CommandInDesktopPackage falhou no próprio processo de diagnóstico. Nenhuma plataforma financeira foi inspecionada ou alterada. A alternativa de executável da instalação retornou acesso negado.

**Conclusão:** pacote registrado, mas acesso nativo não obtido nem validado. Não foi executado Connect/Dial/Answer/Hangup nativo. Não atribuir esses erros ao consentimento do usuário ou declarar DeniedBySystem: a solicitação não chegou a concluir.

O código de sondagem está em tools/native-probe. Pacotes, certificados e registros de máquina permanecem fora do Git. A implementação funcional existente de chamadas ainda depende de UI Automation do Phone Link.

## Verificação posterior — 04/11/2026

Esta verificação substitui a conclusão anterior de que a solicitação de acesso não chegou a executar.

- Novo pacote mínimo PhoneDeck.NativeBridge ativou por IApplicationActivationManager: HRESULT 0, PID válido. GetCurrentPackageFullName retornou 122, comprovando identidade disponível (tamanho do buffer solicitado).
- Capacidades declaradas: runFullTrust, phoneLineTransportManagement e phoneCall. Pacote assinado, instalado para o usuário atual.
- Um transporte HFP encontrado. RequestAccessAsync efetivamente executado: DeniedBySystem.
- RegisterApp executado em uma sondagem separada: UnauthorizedAccessException, HRESULT 0x80070005. Não foi tentado Connect após essa negativa.
- PhoneCallManager.RequestStoreAsync e PhoneLineWatcher funcionam. Duas linhas enumeradas: VoipApp com CanDial=False; Bluetooth com CanDial=True. Isso não comprova que DialWithOptions pode iniciar uma chamada nem que a interface permaneceria oculta.
- Nenhum comando Dial, Answer ou Hangup nativo ou teste de chamada real foi executado.
- As permissões globais phoneCall, phoneCallHistory, bluetooth e microphone estavam Allow. Nenhuma política ou controle de acesso global foi enfraquecido.
- O binário instalado continua 1.9.1.25 com backend Phone Link/UI Automation. A ativação do pacote de diagnóstico não migrou o backend de produção.

Conclusão atual: a ativação do diagnóstico foi resolvida; controle nativo completo de chamadas permanece bloqueado/não validado. Não afirmar independência do Phone Link, nem confundir CanDial=True com teste de discagem bem-sucedido.

Sondagem somente de leitura das linhas: tools/native-probe/NativeLines.cs, versão 0.1.0.17. Precisa de referência System.Runtime.InteropServices.WindowsRuntime.dll para os eventos WinRT, além de System.Runtime.dll e WinMetadata oficiais. Artefatos e logs de máquina continuam fora do Git.

Há relato compatível do mantenedor do projeto examinado: [BestOwl/MyPhone #26](https://github.com/BestOwl/MyPhone/issues/26). Relato externo é contexto; os resultados acima são medições locais. Não foram aplicados patches, tokens de outro aplicativo, ajustes BypassRegistration nem despareamento do telefone.

## Investigação HFP direta — 04/11/2026

- Sondagem TAPI 2.2: lineInitializeExW retornou sucesso, mas Devices=0. Nenhuma linha foi aberta.
- Análise estática dos adaptadores instalados identificou os controles PhoneCall como dependentes de CallsPhoneContract 6; não foram carregadas ou chamadas as bibliotecas do Phone Link no processo do produto. Nenhum token, patch ou desbloqueio de capacidade foi aplicado.
- Caminho alternativo independente: RFCOMM autenticado ao serviço Bluetooth Hands-Free Audio Gateway (0x111F), com sondagem de capacidades BRSF/CIND/CMER. Não envia ATD, ATA, AT+CHUP ou comandos de áudio. Aborta ao detectar chamada ativa ou recebida.
- Descoberta em cache encontrou um gateway, mas conexão RFCOMM expirou. Descoberta atualizada não disponibilizou serviço. Última medição BluetoothDevice.ConnectionStatus: iPhone desconectado; o adaptador TP-Link permanece presente e OK. Isso impede concluir a validação no aparelho neste momento; não comprova impossibilidade permanente de HFP direto.
- Implementado HfpProtocol com comandos de discar/atender/desligar, enumeração dinâmica de indicadores, validação de número, bloqueio de discagem durante chamada e bloqueio de encerramento ambíguo. Test-HfpProtocol passou em transporte simulado: não executou qualquer chamada real.
- A classe de protocolo ainda não está ligada aos comandos do produto instalado. Nenhuma nova versão do PhoneDeck foi entregue nesta rodada; a instalação continua 1.9.1.25 e depende de Phone Link/UI Automation.
- Áudio SCO no desktop não foi implementado ou validado para o caminho RFCOMM. Confirmar handshake não comprova voz na caixa de som/microfone e não basta para declarar a integração concluída.

Para repetir apenas a conexão diagnóstica com o iPhone próximo, Bluetooth ligado e disponível: scripts/test-native-connection.ps1 -Transport Hfp. O script compila a sondagem identificada, registra hashes localmente e nunca muda o backend instalado. Não altera pareamento, drivers, registro, BIOS nem serviços. Para TAPI, usar -Transport Tapi.

Referências primárias: documentação Microsoft de lineInitializeExW/lineGetDevCapsW; protocolo HFP do Bluetooth SIG; implementação e medições do repositório Tobse2910/Call-Klingel. Resultados externos não substituem a medição no iPhone deste usuário.

## Validação com iPhone desbloqueado — 05/11/2026

O usuário confirmou proximidade e desbloqueio. As verificações locais ainda retornaram rádio Bluetooth On e BluetoothDevice.ConnectionStatus=Disconnected para o iPhone.

- Descoberta atual de HFP: Success, Services=0.
- Entrada de serviço em cache presente; descriptor SDP indica canal RFCOMM 8. Isso é informação salva, não confirmação de disponibilidade atual.
- Conexão WinRT pelo serviço salvo: 0x8007277C, serviço não encontrado no namespace.
- Sondagem Winsock AF_BTH com autenticação e criptografia e canal obtido do SDP: NetworkUnreachable. Não percorreu portas nem tentou canais adivinhados.
- Nenhum handshake HFP com o aparelho completou nesta rodada; nenhum comando de discar, atender, desligar ou rotear áudio foi enviado.
- Foi solicitado ao usuário verificar/conectar o PC em Ajustes > Bluetooth no iPhone. Não foi removido o pareamento, reiniciado serviço ou alterado driver.
- Test-HfpProtocol passou novamente, incluindo o descritor SDP real (sem endereços) e rejeição de descritores inválidos/ambíguos. Esses testes continuam simulados, sem comprovação de voz.
- O backend instalado não foi substituído. A versão em produção continua 1.9.1.25, dependente da ponte Phone Link.

Scripts disponíveis: test-native-connection.ps1 -Transport Hfp -Status (somente rádio/estado), -Transport Hfp -Cached (serviço salvo), -Transport RawHfp (Winsock com protocolo de diagnóstico limitado ao handshake). Cada compilação diagnóstica registra as fontes efetivamente usadas e o hash do executável; não gera nova entrega do PhoneDeck.

## Chamada recebida sem encaminhamento — 05/11/2026

Após o usuário relatar chamada que não chegou ao PC, o vínculo geral indicou Conectado, mas a área Chamadas exibiu: Não foi possível conectar ao seu dispositivo móvel. Rádio On, iPhone Classic Disconnected, serviços Bluetooth/PhoneSvc em execução. Tentar novamente foi solicitado uma única vez pelo controle verificado de reconexão; o erro persistiu. Nenhuma chamada foi discada/atendida/encerrada. Versão 1.9.2.26 corrige somente a falsa aparência de disponibilidade no PhoneDeck, mostrando Chamadas desconectadas em vermelho e o motivo. A conexão do iPhone ainda precisa ser restabelecida.

## Confirmação visual de conexão geral — 05/11/2026

O usuário mostrou o iPhone Conectado na interface do Windows. A leitura simultânea manteve vínculo geral Conectado e erro no discador Não foi possível conectar ao seu dispositivo móvel; Bluetooth Classic Disconnected. Há registros presentes tanto LE quanto Classic; presença no PnP não comprova conexão HFP. Reconexão com a janela ativa não resolveu. Em Configurações > Recursos, Chamadas e notificações estavam On. O recurso Chamadas foi reinicializado uma vez e confirmado On ao final, sem alterações nos demais controles e sem chamadas ativas detectadas. O erro persistiu. A tela Dispositivos foi preparada sem remover vínculo; foi solicitada ao usuário a etapa de Esquecer Este Dispositivo no iPhone para completar novo pareamento. O registro no PC ainda foi preservado. Documentação atual de suporte confirma compatibilidade de Phone Link/iPhone com Windows 10; não atribuir a falha à versão do Windows sem evidência adicional. Fonte: https://support.microsoft.com/en-us/windows/apps/phonelink/phone-link-requirements-and-setup

Nenhuma chamada real foi iniciada, atendida ou encerrada; o backend nativo continua incompleto.

## Novo pareamento iniciado — 05/11/2026

Usuário confirmou Esquecer Este Dispositivo no iPhone. O registro antigo deixou de aparecer no Phone Link e os nós Bluetooth presentes do iPhone não foram encontrados na verificação seguinte. O tablet existente foi preservado. Adicionar dispositivo > iPhone foi iniciado, e a tela oficial Emparelhe seus dispositivos com Bluetooth exibiu instruções de QR válido por 3 minutos. A leitura do QR e a confirmação do código nos dois aparelhos foram solicitadas ao usuário. Não registrar o reparo como concluído antes da confirmação e validação do canal de chamadas. Nenhuma chamada foi executada.

## Permissões confirmadas e perfil HFP habilitado — 05/11/2026

Usuário confirmou QR/código e depois as permissões do iPhone. Assistente oficial confirmou pareamento. Compartilhamento opcional de arquivos foi ignorado; tablet existente preservado. O novo pareamento geral não foi anunciado como controle nativo.

Verificações novas:
- Driver TP-Link assinado, versão 1.9.1038.3023; Bluetooth Classic e BLE anunciados como suportados. BthHFAud/BthHFEnum/RFCOMM ativos.
- API Win32 exclusiva de Classic: iPhone autenticado e lembrado, mas desconectado; serviços habilitados inicialmente zero.
- BluetoothSetServiceState para o serviço remoto HFP Audio Gateway 0x111F retornou 0. Verificação independente confirmou serviço habilitado e dispositivos iPhone Hands-Free HF (System) e Hands-Free HF Audio (MEDIA), ambos OK. Nenhum outro perfil foi alterado.
- O diálogo antigo que propunha remover novamente o pareamento foi cancelado. Recurso Chamadas permaneceu habilitado. Phone Link foi reiniciado uma vez após a instalação do perfil, com ausência de chamada verificada; outros programas e pareamentos foram preservados.
- Pacote próprio PhoneDeck.NativeBridge 0.1.0.31 ativou com identidade válida. RequestAccessAsync novamente DeniedBySystem; RegisterApp não foi chamado após a negativa. Não se trata de consentimento pendente do usuário.
- Sondagem RFCOMM própria continuou NetworkUnreachable, inclusive em tarefa temporária na sessão normal do usuário; tarefa removida ao terminar.
- Encontrado e corrigido erro em Endpoint.Create da sondagem: ela devolvia o destino remoto também para endereços locais. Test-RfcommEndpoint reproduziu a falha antes e passou após a correção. A correção não eliminou o erro de conexão real. Sondagens com conexão síncrona e verificação de endereço coerente com SDP também não completaram handshake.
- Nenhum comando de discar, atender, desligar ou rotear áudio foi executado. Não há validação de voz.

O usuário reiterou que quer tudo no PhoneDeck/Ulanzi e observou que o QR utilizado foi do Windows. Essa observação procede: o QR do produto continua um guia local, não um pareamento nativo. O backend instalado 1.9.2.26 continua dependente do Phone Link; perfil HFP habilitado e testes de protocolo não significam substituição concluída. Não repetir a solicitação de permissões já confirmadas nem afirmar que um novo QR genérico resolveu a telefonia.

## Auditoria do pedido de desbloqueio — 05/11/2026

Usuário autorizou verificar e aplicar desbloqueio suportado. As permissões globais phoneCall de usuário e máquina estavam Allow, sem política AppPrivacy de negação encontrada. O pacote PhoneDeck.NativeBridge foi atualizado para diagnóstico 0.1.0.36, incluindo DeviceCapability bluetooth. A sondagem passou a consultar AppCapability.CheckAccess e só solicita consentimento se UserPromptRequired; não registra transporte após negativa.

Medição no próprio processo com identidade válida:
- phoneCall: Allowed.
- phoneLineTransportManagement: Allowed.
- DeviceAccessInformation do transporte HFP: Allowed.
- PhoneLineTransportDevice.RequestAccessAsync: DeniedBySystem.
- IsRegistered: False.

AppCapability.Create("bluetooth") retornou E_INVALIDARG; bluetooth é declaração de DeviceCapability e essa consulta genérica não deve ser interpretada como negação de Bluetooth. A declaração de Bluetooth foi aceita no pacote. Não apareceu consentimento pendente que pudesse ser concedido.

Não foi identificado desbloqueio local documentado que resolva a negativa do método. Declaração de capacidade restrita/consentimento e a decisão de acesso dessa API são verificações distintas. Não afirmar que é necessária aprovação de Store, token LAF específico ou que a API é exclusiva da Microsoft sem fonte oficial aplicável ao caso. Nenhuma política global foi enfraquecida e nenhum patch/token de outro aplicativo foi aplicado. Não houve chamadas.

Situação: permissões do pacote conferidas; controle nativo continua não liberado. Produto instalado continua 1.9.2.26; diagnóstico não substitui backend de produção. Referências: https://learn.microsoft.com/en-us/uwp/api/windows.security.authorization.appcapabilityaccess.appcapability.checkaccess e https://learn.microsoft.com/en-us/answers/questions/6030086/phonelinetransportdevice-requestaccessasync-return

## Recuperação priorizada após autorização — 05/11/2026

Usuário autorizou fazer o necessário após a proposta de restabelecer primeiro as chamadas no PC.

Ações e resultado:
- Reinicialização administrativa somente do adaptador USB TP-Link VID2357/PID0604: pnputil retornou 0. PC não foi reiniciado; nenhum pareamento foi apagado.
- Driver oficial de uso geral baixado e inspecionado: Win10 x64 1.9.1038.3015, compatível com o hardware e catálogo Microsoft válido. É anterior ao instalado 1.9.1038.3023; não foi feito downgrade nem instalação de driver de Windows 11/7.
- Microsoft Store/winget: nenhum upgrade disponível para 9NMPJ99VJBWV; versão permanece 1.25072.81.0.
- Registro do pacote oficial reparado com Add-AppxPackage -Register -DisableDevelopmentMode: Status Ok, dados e pareamentos preservados.
- Encontrada entrada específica de consentimento phoneCall do Microsoft.YourPhone em Prompt. Concedido Allow somente a esse pacote, com valor anterior registrado localmente, conforme autorização do usuário para chamadas. O estado Allow persistiu após reiniciar o aplicativo.
- Mesmo após essas alterações e reconexão solicitada, a área Chamadas permaneceu sem DialPaneGrid e com Tentar novamente. Classic continua autenticado/lembrado, HFP habilitado, mas desconectado. A recuperação ainda não foi comprovada.
- Solicitada informação física: quais dispositivos aparecem Conectado no iPhone, para verificar possível concorrência com outro fone/carro/PC. Não inferir concorrência sem a resposta.

## PhoneDeck 1.9.3.27 — indicador de disponibilidade

Corrigido falso positivo adicional: vínculo geral Conectado não representa discador disponível. Snapshot considera aba selecionada, presença do discador e erro explícito. Aba diferente fica Sem verificação; página de configuração sem discador fica Chamadas indisponíveis. Discador disponível é azul, não afirma chamada/áudio validados. Removida a recomendação automática repetitiva de reconectar o iPhone.

Test-CallReadiness e Test-Window passaram sem dispositivos/chamadas/alteração de áudio. Snapshot real confirmou DeviceConnection=Conectado e CallsState=Unavailable. Mesmo artefato instalado nos dois aliases por hash; backup imediato 1.9.2.26 preservado. Atalhos e organização da Ulanzi não foram alterados. O backend permanece Phone Link/UI Automation; integração nativa não concluída. Nenhuma chamada foi iniciada, atendida ou encerrada.

## Transporte nativo conectado — 05/11/2026

Após o usuário reiniciar o Bluetooth pelos Ajustes do iPhone e confirmar que somente o PC aparece conectado, houve um intervalo de descoberta sem vínculo enumerado. Na leitura seguinte, Bluetooth Classic confirmou Connected=True, Remembered=True e Authenticated=True. Não foi necessário novo QR, remoção de pareamento ou troca de driver. A ausência transitória anterior não deve ser tratada como perda comprovada do pareamento.

Resultados reais, sem chamadas:
- HfpProbe 0.1.0.35: descoberta atual Success/Services=1; RFCOMM conectado; BRSF/CIND/CMER responderam OK, handshake completo. A sondagem não valida o conteúdo dos indicadores nem áudio.
- NativeBridge 0.1.0.36: RequestAccessAsync passou a Allowed; registro próprio concluído. Isso substitui a medição anterior DeniedBySystem para este estado atual, mas não comprova qual das ações anteriores foi determinante.
- Diagnóstico atualizado NativeBridge 0.1.0.37: identidade válida; capacidades phoneCall e phoneLineTransportManagement Allowed; transporte Allowed e RegisteredAfter=True; ConnectAsync retornou True; watcher completou com uma linha Bluetooth CanDial=True e Network=Unknown. Não interpretar CanDial como chamada realizada ou áudio validado.
- Fonte tools/native-probe/NativeAccess.cs agora testa conexão somente após Allowed e ausência de chamada ativa/recebida; enumera linhas sem comandos de discagem, atendimento ou encerramento.
- Pacote 0.1.0.37 assinado com certificado próprio existente, versão/hash instalados conferidos, pacote imediatamente anterior 0.1.0.36 preservado. Recibo privado out/native-access-result.local.json; resultado privado em LocalAppData/PhoneDeck/native-access.local.txt.
- Drivers MEDIA Hands-Free HF Audio e A2DP do iPhone presentes e OK. Presença do driver não prova rota de voz.

Limites atuais: o aplicativo instalado continua PhoneDeck 1.9.3.27 e os atalhos Ulanzi ainda usam Phone Link/UI Automation. Não foi promovido o diagnóstico a backend de produção. Ainda faltam integração dos comandos de chamada, verificação do controle atender/desligar disponível neste Windows 10 e rota de áudio. As APIs públicas enumeradas no Windows 10 não incluem a classe PhoneCall das versões mais recentes; PhoneLine.Dial disponível não comprova sozinho os três comandos sem janela externa. A referência BestOwl/MyPhone usa PhoneLine.Dial, mas os arquivos consultados não implementam os três controles próprios desejados. Não reutilizar tokens ou binários privados de outro aplicativo.

Nenhuma ligação real foi iniciada, atendida ou encerrada. Mantida a escolha do usuário: validar sem fazer chamada.

## Integração HFP direta — PhoneDeck 1.10.0.28 — 05/11/2026

Usuário pediu integrar Ligar/Atender/Desligar no PhoneDeck/Ulanzi sem abrir o app Windows e sem chamada de teste. Instalado o mesmo artefato nos aliases PhoneDeck.exe e LigarTerra.exe; backup imediato 1.9.3.27 preservado pelo instalador scripts/install.ps1. Manifesto de fontes e hash em out/build.json; reserva/testes/implantação em out/build-28.local.json. Estado publicamente distribuído no GitHub não foi alterado nesta rodada.

Implementação:
- NativeHfpBridge: servidor local com pipe limitado ao SID atual e uma sessão persistente RFCOMM autenticada. Descobre exatamente um gateway HFP, sem canal adivinhado, launcher tel:, UI Automation de chamadas ou fallback Phone Link.
- NativeHfpSession/HfpStreamChannel: seleção do gateway, negociação e respostas limitadas por tamanho/tempo. Não reconecta com PhoneCallManager.IsCallActive/IsCallIncoming; protege chamadas existentes e não tenta novamente comandos após timeout.
- HfpCommandDispatch liga os mesmos comandos da janela/CLI/Ulanzi ao HfpProtocol. Ligar exige estado ocioso; Atender exige entrada única sem ativa/retida; Desligar exige chamada única em CLCC. Nenhuma chamada é enviada automaticamente.
- Snapshot e Atualizar usam o próprio serviço e avisam áudio pendente. Não leem mais a janela ou o histórico do Phone Link. Os arquivos antigos de histórico não foram apagados; o novo modo não importa histórico do celular.
- Runtime Ulanzi: seis contatos apontam PhoneDeck.exe; posição 1_2 --atender e 2_2 --desligar, rótulos corretos. Atalhos apenas lidos, nunca executados para teste. Ícones/layout/relógio/pastas preservados.

Validação sem chamadas:
- Test-HfpProtocol usa as classes do executável exato entregue e um telefone simulado. Passaram negociação, indicadores reordenados, rotas de produção status/dial/answer/hangup, comandos únicos, rejeição de número injetado, telefone ocupado, múltiplas chamadas, telefone ocioso sem chamada a atender/desligar, comando desconhecido e erro sem repetição.
- Test-Window passou com contatos fictícios e sem alterar áudio. Test-CallReadiness passou, para compatibilidade do utilitário de diagnóstico anterior.
- Sondagem real HfpProbe abortou ao detectar chamada ativa/recebida. Após instalar, snapshot do novo backend também bloqueou a conexão inicial pelo mesmo motivo; nenhum comando HFP de chamada foi enviado. Não remover esse bloqueio para forçar teste.
- PhoneDeck próprio aberto. Servidor HFP permanece local aguardando ações; ainda sem sessão RFCOMM própria estabelecida na versão instalada devido ao bloqueio acima.

Limitação material: controle de chamadas integrado não entrega áudio SCO no PC. O backend direto não implementa a rota de voz para caixa/microfone do Windows. APIs PhoneCall e CallsPhoneContract 6 ausentes neste Windows 10, verificado por ApiInformation. Portanto a substituição completa de viva-voz continua incompleta; não declarar o produto pronto para chamadas de segurança de trading. Foi perguntado ao usuário se o telefone realmente está em chamada agora, para distinguir chamada real de estado global obsoleto sem enviar comandos de ligação.

## Após encerramento da chamada pelo usuário — 05/11/2026

Usuário confirmou que havia chamada real e que a encerrou. A proteção de chamada ativa deixou de bloquear, mas o snapshot da versão instalada 1.10.0.28 continuou Unavailable. HfpProbe: rádio On, descoberta HFP Success/Services=1, iPhone Classic Disconnected, conexão RFCOMM falhou com rede inacessível. NativeBridge 0.1.0.37 manteve Access Allowed/RegisteredAfter True, porém ConnectAsync retornou False nesta tentativa. CanDial True da linha não deve ser interpretado como conexão pronta.

Após nova checagem de ausência de chamada, foi fechado somente o processo PhoneExperienceHost com caminho validado pelo pacote oficial, para eliminar possível disputa de perfil. Não foi reaberto. Snapshot próprio continuou com falha de transporte. Win32 confirmou vínculo Remembered=True/Authenticated=True, Connected=False. Não houve remoção de pareamento, alteração de driver, chamada nem comando de áudio. Solicitado ao usuário tocar no PC Lucca em Ajustes > Bluetooth do iPhone e informar o resultado, pois a tentativa de reconexão pelo PC não estabeleceu o vínculo. Integração de comandos permanece instalada, porém conexão/funcionamento real e áudio no PC não estão comprovados.

## Canal direto real validado após conexão pelo iPhone — 05/11/2026

Usuário tocou no PC Lucca nos Ajustes Bluetooth e confirmou Conectado. Snapshot da instalação PhoneDeck 1.10.0.28 passou a Backend=HFP direto, CallsState=ControlConnected e Connection=Controle conectado · áudio pendente. A sessão do processo próprio conseguiu executar a negociação com validação dinâmica dos indicadores e leitura de estado; não foi apenas o diagnóstico isolado. Nenhum comando dial/answer/hangup foi enviado. Controle dos três comandos continua validado em telefone simulado; áudio SCO próprio continua não implementado/validado. Não declarar conversa pelo PC pronta. A reconexão automática após o encerramento de uma chamada ainda não foi comprovada, pois esta retomada exigiu toque manual no iPhone.

## Investigação do áudio próprio — 05/11/2026

Pedido explícito: usar microfone e caixa do PC pelo PhoneDeck, sem abrir Phone Link e sem chamada de teste. Nenhuma chamada foi iniciada/atendida/encerrada; nenhum áudio do microfone foi capturado ou salvo nesta rodada.

Evidências novas:
- Enumeração Core Audio de todos os estados encontrou entrada Microfone/ME6S ativa, saída Alto-falantes/ME6S ativa e outras saídas de monitores/Realtek. Não há endpoints WASAPI de voz HFP do iPhone publicados na lista; a entrada A2DP do iPhone não constitui caminho bidirecional de voz.
- Driver do nó iPhone Hands-Free HF Audio: Microsoft BthHFAud, microsoft_bluetooth_hfp.inf, 10.0.19041.5794. Nenhum driver foi instalado ou substituído.
- AudioRoutingManager de Windows.Phone.Media.Devices e PhoneCall ausentes via ApiInformation; AudioPlaybackConnection presente, mas é caminho de recepção A2DP e não resolve microfone de chamada.
- Novo AudioPinProbe 0.1.0.1, somente GET em Kernel Streaming: uma interface de áudio HFP presente; quatro pinos; pinos 0/3 anunciam PCM mono 16 bits/8 kHz, pinos 1/2 são conexões de ponte. Não criou fluxo nem enviou comandos de chamada.
- Novo AudioStreamProbe: 0.1.0.1 abriu ambos os pinos PCM com KsCreatePin retornando 0, mantendo STOP. Revisão 0.1.0.2 verificou transições ACQUIRE e PAUSE com sucesso; RUN retornou erro Win32 1 (Função incorreta) para os dois pinos. Pinos retornaram a STOP e handles foram fechados. Nenhuma amostra foi reproduzida/capturada. Artefatos e hashes diagnósticos registrados em out/audio-pin-build.local.json e out/audio-stream-build.local.json, versão imediatamente anterior do segundo teste preservada.
- Na mesma situação, Classic Bluetooth confirmou iPhone Remembered/Authenticated=True, Connected=False. Portanto não concluir que RUN é permanentemente indisponível: o teste falhou com o vínculo remoto desconectado. A captura mostrada pelo usuário é compatível com o status real Unavailable do snapshot.

Foi solicitado ao usuário conectar Lucca pelo Bluetooth do iPhone e manter a tela de Ajustes aberta, sem ligação, para verificar ativação de voz com vínculo ativo. Não foi pedido novo pareamento, QR nem repetição de permissões. Próximo passo: quando confirmado conectado, verificar estado Classic e ativação do transporte próprio; repetir a sondagem de estados sem amostras, com telefone ocioso. Somente após resolver RUN implementar/promover a ponte de amostras microfone -> pino PCM de envio e pino PCM recebido -> saída selecionada. Abrir handles em STOP/PAUSE não comprova canal SCO nem áudio funcional.

Instalação de produção continua 1.10.0.28. Nenhuma nova versão do PhoneDeck ou integração de áudio foi instalada. Não declarar o áudio pronto. Não foi aberto o Vincular ao Celular, alterado padrão/volume de áudio, gravado microfone, aplicado driver experimental ou executada chamada.

Fontes primárias examinadas: https://learn.microsoft.com/en-us/windows-hardware/drivers/bluetooth/using-the-bluetooth-driver-stack ; https://learn.microsoft.com/en-us/uwp/api/windows.applicationmodel.calls.phonelinetransportdevice.audioroutingstatus ; definições de KS https://github.com/microsoft/win32metadata/blob/main/generation/WinSDK/RecompiledIdlHeaders/shared/ks.h . Os resultados acima são medições locais; as fontes não comprovam que a ponte de voz do produto funcione.

## Áudio com iPhone confirmado conectado — 05/11/2026

Usuário confirmou Conectado e tela de Ajustes aberta. Win32 confirmou Classic Connected=True, Remembered=True, Authenticated=True. Portanto a falha de RUN observada agora não pode ser atribuída apenas ao vínculo desconectado.

Resultados sem chamadas e sem amostras:
- AudioStreamProbe 0.1.0.2: KsCreatePin(0/3)=0; ACQUIRE/PAUSE OK; RUN falhou nos dois canais com Win32 1 (Função incorreta). Ao finalizar, solicitado STOP e fechados os handles.
- NativeBridge 0.1.0.37, enquanto o servidor HFP direto do PhoneDeck estava aberto: Access Allowed, RegisteredAfter True, NativeConnected False.
- Após verificar telefone ocioso por HfpProbe --status, foi encerrado somente o processo próprio PhoneDeck.exe --native-server, identificado por ExecutablePath exato e CommandLine. Nenhum outro aplicativo foi parado, nenhuma chamada alterada.
- Após liberar essa sessão, NativeBridge próprio retornou NativeConnected True e linha Bluetooth CanDial True. Isso é evidência de possível disputa entre controle RFCOMM próprio e transporte HFP do sistema; não provar causalidade exclusiva por uma medição sequencial.
- Com o transporte nativo conectado, a sondagem de áudio continuou com RUN recusado para os dois canais. Não houve ligação de teste, captura/reprodução de áudio ou abertura do Phone Link. Não instalar driver ou enfraquecer segurança a partir desse resultado.

Conclusão desta rodada: a instalação 1.10.0.28 ainda não entrega voz bidirecional. Os pinos PCM existem e abrem, mas a ativação do canal de voz não está resolvida. Não é correto anunciar o microfone/caixa como integrados nem dizer que nova confirmação Bluetooth concluirá o áudio. O backend precisa coordenar o transporte SCO e o controle HFP; a implementação atual só cobre sinalização RFCOMM. A hipótese de identidade/permissão da sondagem KS e o comportamento restrito à chamada real ainda não foram excluídos; nenhuma chamada pode ser feita para verificá-los sem nova autorização específica. Não afirmar impossibilidade permanente do hardware nem necessidade comprovada de trocar Windows/driver.

## Ajuste solicitado: acesso empacotado e falhas explícitas — 05/11/2026

Escopo mantido: nenhuma chamada de teste; não abrir Phone Link; não capturar/reproduzir som; preservar plataformas financeiras.

- AudioPinProbe 0.1.0.2 consultou interfaces, meios e comunicação dos quatro pinos reais. O driver anuncia KSINTERFACE_STANDARD_STREAMING id 0 e meio Standard id 0, iguais aos usados. Pino 0 SINK, pino 3 BOTH. Não foi presumido outro modo de streaming.
- AudioStreamProbe 0.1.0.3 testou os dois pinos simultaneamente: ambos abrem, passam ACQUIRE/PAUSE e falham em RUN com Win32 1. Handles fechados. Abertura duplex não resolveu.
- NativeBridge atualizado somente como diagnóstico para 0.1.0.38, com NativeAudioAccess.cs e cópia adaptada do teste de pinos, mesma identidade/capacidades e certificado existentes. Pacote anterior 0.1.0.37 preservado. Versão/hash do executável instalado conferidos. Houve falha na geração do recibo por uso de Get-FileHash em pipeline com strings no PS5; corrigida e recibo refeito sem recompilar/reinstalar nem sobrescrever o pacote anterior. Fonte e artefato identificados em out/native-access-result.local.json.
- Medição dentro do pacote próprio: Identity=122, Access=Allowed, Registered=True, NativeConnected=True. A ativação dos dois pinos ainda foi recusada. Assim, identidade de pacote ausente não explica sozinha a falha observada. PinProbeExit=0 na revisão embutida significa apenas execução concluída, não voz disponível.
- Corrigido esse código de saída no AudioStreamProbe 0.1.0.4: agora VoiceRunReady=False e ExitCode=3 quando RUN falha. Verificação real confirmou ExitCode=3; os dois canais foram fechados. Versão anterior 0.1.0.3 preservada, hashes atualizados em out/audio-stream-build.local.json. O pacote diagnóstico 0.1.0.38 permanece com o teste anterior; não confundir seus contadores.

Resultado: ajustes de diagnóstico concluídos, áudio do produto NÃO concluído. A instalação PhoneDeck permanece 1.10.0.28, sem voz SCO implementada. Permissão/registro/conexão do transporte e abertura de pinos não são comprovação de fluxo de voz. Não concluir necessidade de reinstalar driver, reinstalar Windows ou realizar nova conexão manual a partir desses testes. Permanecem por investigar a ativação SCO/gerenciamento da sessão de voz e seu comportamento sem chamada ativa; nenhuma chamada será feita para essa investigação sem nova autorização específica. Servidor RFCOMM próprio havia sido liberado na rodada anterior para eliminar concorrência; ele não foi reiniciado automaticamente nesta rodada.

## REA e proposta de navegador — 05/11/2026
Resultado consolidado em ARQUITETURA-VOZ.md: chamadas do aplicativo instalado delegam à telefonia Windows; interface própria completa depende de APIs ausentes nesta instalação Windows 10. App iOS Link to Windows é opcional; browser/WebRTC não dá acesso ao áudio da ligação do chip. Nenhuma aplicação foi aberta, instalada no iPhone ou ligação feita. Backend próprio de voz continua pendente.

## Atualização: backend nativo com identidade própria

A versão 1.11.0.31 substitui o socket RFCOMM separado por SystemHfp/SystemHfpDispatch. O pacote PhoneDeck.NativeBridge tem a mesma assembly testada da janela e executa o servidor em MTA, sem interface da Microsoft. HfpTransportDevice é ativado via IActivationFactory.ActivateInstance e consulta da interface statics, não por solicitação direta da factory statics.

IIDs e slots foram conferidos nos metadados COM e símbolos públicos da versão instalada de BTAGService.dll. SetAudioTransferAsync(true) chama ConnectAudioAsync; false chama DisconnectAudioAsync. Não copiar DLLs, símbolos ou código do sistema para a distribuição. A API é interna, portanto falha fechada se indisponível.

Evidência local: acesso Allowed, registro próprio, ConnectAsync true, lista de chamadas vazia e preparação de áudio com resultado 0. Entrada ME6S e saída Realtek são os dispositivos padrão de mídia e comunicação, microfone ligado. Não foram feitas chamadas nem captura de microfone. Isso prova preparação aceita, não áudio de conversa. O teste de pinos KS continua sem RUN durante repouso e não é usado como backend.

O broker usa novo pipe por usuário e ativa somente a identidade PhoneDeck. Comandos de chamada usam métodos nativos, guardas e prazo por solicitação; não repetem comandos após falha de áudio. Test-SystemHfp valida o mesmo dispatcher do binário com transporte simulado. Os resultados anteriores deste documento descrevem etapas históricas, não o backend atual.