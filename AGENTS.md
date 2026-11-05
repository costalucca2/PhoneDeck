# Desenvolvimento

Código local. QRCoder 1.8.1 (MIT) é baixado pelo build com versão/hash fixos e embutido no executável; runtime não depende de servidor externo. Faça testes sem chamadas por padrão. Nunca ligue para contatos para testar sem autorização específica. Não leia ou escreva dados de plataformas financeiras.

Antes de compilar ou entregar, aplique a política de versões do responsável; neste ambiente a fonte central é `Projetos/skills/versoes-builds/SKILL.md`. Identidade embutida em src/Program.cs; build.json registra fontes e artefato. Preserve instalação atual e imediatamente anterior. Não publique contatos, diretórios de runtime, logs ou capturas.

Ao concluir uma operação de UI, diferencie comando enviado de ligação conectada. Somente uma ativação por solicitação; nenhuma repetição automática.


Backend padrão desde 1.10.0.28: HFP direto por NativeHfpBridge/NativeHfpSession. Nenhum fallback para Phone Link/URI/clique nos comandos de chamada. Áudio SCO no PC não entregue; não afirmar viva-voz completo. Inicializar sessão somente com telefone ocioso; não remover proteção de chamada ativa para conseguir testar. O servidor local mantém a sessão entre ações. Test-HfpProtocol testa as classes do binário construído com telefone simulado. Installer scripts/install.ps1 promove apenas artefato marcado tested e mantém backup imediato.

Publicação experimental: scripts/build.ps1 -> scripts/verify.ps1 -> scripts/install.ps1. verify gera o recibo de testes usado pelo instalador; os testes só compilam diagnósticos e não acessam o telefone. Instalação padrão: LocalAppData/PhoneDeck; -Runtime atualiza instalação existente. CI em .github/workflows/windows.yml. Não publicar a prévia como telefonia/áudio completos. --native-stop só encerra servidor ocioso; não é Hangup.
