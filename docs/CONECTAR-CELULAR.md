# Conectar um celular ao PhoneDeck — modo local

O assistente do PhoneDeck gera um QR que abre um guia temporário no navegador do celular, na mesma rede do PC. Não requer domínio, conta, servidor na nuvem ou aplicativo instalado no celular.

**O computador precisa permanecer ligado.** O QR abre o guia: ele não autoriza Bluetooth, não faz login, não concede acesso à telefonia e não conecta o celular automaticamente.

## No computador

1. Compile com `scripts/build.ps1` e abra `out/PhoneDeck.exe`.
2. Escolha **Conectar celular · QR**, no cartão do aparelho. Também pode usar `PhoneDeck.exe --setup`.
3. Se o Firewall impedir acesso pelo celular, execute como administrador, usando o caminho real do seu executável:

```powershell
.\scripts\enable-local-setup.ps1 -ProgramPath 'C:\Caminho\PhoneDeck.exe'
```

Essa regra permite apenas o executável indicado, TCP 40521, rede privada e sub-rede local. Ela não permite rede pública. Não altere a classificação de uma rede que você não conhece apenas para usar o guia.

## No celular

1. Conecte-se ao mesmo Wi-Fi/rede local do computador.
2. Leia o QR pela câmera e abra a página. O QR expira em 10 minutos; fechar o assistente encerra a página.
3. No PC, use **Bluetooth do Windows → Adicionar dispositivo**.
4. No iPhone/Android, abra Bluetooth, mantenha a tela aberta e confirme o mesmo código nos dois aparelhos.
5. Quando disponível, permita chamadas e sincronização de contatos nas opções do computador pareado.

No iPhone: **Ajustes → Bluetooth → ⓘ** ao lado do PC. No Android: **Configurações → Bluetooth → engrenagem** ao lado do PC; nomes variam por fabricante.

## O que é transmitido

Somente uma página estática de instruções. O endereço contém um token aleatório temporário. Não há rotas de discagem, leitura de contatos, login, senha ou controle remoto. A página não faz requisições a serviços externos.

A mensagem **Guia aberto** indica uma visita à página, não uma conexão Bluetooth. O guia também pode ser aberto no próprio PC para teste.

## Quando não abre

- Confirme que os dois aparelhos estão na mesma rede. Wi-Fi de convidados pode bloquear a comunicação entre dispositivos.
- Mantenha o assistente aberto e gere outro QR se tiver expirado.
- Verifique a regra do Firewall e se a rede de confiança está marcada como privada.
- Feche outro assistente PhoneDeck que esteja usando a porta 40521.

## Estado das chamadas

O aplicativo ainda usa Microsoft Phone Link para as chamadas funcionais existentes. O controle Bluetooth nativo sem abrir sua janela está em investigação; o registro do pacote de diagnóstico não comprova acesso à telefonia. O assistente QR não resolve essa limitação sozinho.
