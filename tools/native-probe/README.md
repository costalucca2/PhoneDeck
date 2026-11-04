# Diagnóstico de telefonia Bluetooth nativa

NativeProbe.cs faz somente enumeração e consulta de identidade/registro. NativeAccess.cs solicita acesso e tenta registrar o app no transporte quando Allowed; não faz Connect/Dial/Answer/Hangup. Não execute NativeAccess sem consentimento explícito do usuário para registrar/solicitar acesso à telefonia.

Os diagnósticos não fazem parte do executável principal e não são executados pelos testes de interface ou do guia QR. O manifesto e a assinatura precisam de uma identidade Windows apropriada, e o teste deve distinguir pacote registrado, identidade efetiva no processo, acesso Allowed e IsRegistered verdadeiro.

Consulte docs/BLUETOOTH-NATIVO.md antes de tentar integrar esses resultados ao backend. As primeiras tentativas de registro/ativação não obtiveram acesso de telefonia; não interpretar instalação do pacote como sucesso da API.
