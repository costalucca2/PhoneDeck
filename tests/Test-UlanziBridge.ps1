param([string]$Executable=(Join-Path $PSScriptRoot '..\out\PhoneDeck.exe'))
$ErrorActionPreference='Stop'
& node --check (Join-Path $PSScriptRoot '../integrations/ulanzi/com.lucca.recording.ulanziPlugin/app.js')
if($LASTEXITCODE){throw 'Ulanzi bootstrap syntax failed'}
& node (Join-Path $PSScriptRoot 'Test-UlanziProtocol.js') (Resolve-Path $Executable).Path
if($LASTEXITCODE){throw 'Ulanzi WebSocket protocol fixture failed'}