#Requires -RunAsAdministrator
param([Parameter(Mandatory=$true)][string]$ProgramPath)
$ErrorActionPreference='Stop'
$resolved=(Resolve-Path -LiteralPath $ProgramPath).Path
if([IO.Path]::GetFileName($resolved) -ne 'PhoneDeck.exe'){throw 'Informe o caminho do executável PhoneDeck.exe.'}
$rule=Get-NetFirewallRule -Name 'PhoneDeck.LocalSetup' -ErrorAction SilentlyContinue
if($rule){if($rule.Group -ne 'PhoneDeck'){throw 'Existe uma regra de outro aplicativo com esse nome.'};$rule|Get-NetFirewallApplicationFilter|Set-NetFirewallApplicationFilter -Program $resolved|Out-Null}
else{New-NetFirewallRule -Name 'PhoneDeck.LocalSetup' -DisplayName 'PhoneDeck - guia local de configuração' -Group 'PhoneDeck' -Direction Inbound -Action Allow -Enabled True -Profile Private -Program $resolved -Protocol TCP -LocalPort 40521 -RemoteAddress LocalSubnet -EdgeTraversalPolicy Block|Out-Null}
Write-Output 'Guia local permitido somente para PhoneDeck.exe, TCP 40521, rede privada e sub-rede local.'
