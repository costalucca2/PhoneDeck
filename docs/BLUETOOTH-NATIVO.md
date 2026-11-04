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
