param([string]$OutputDirectory=(Join-Path $PSScriptRoot '..\out'))
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$directory=(Resolve-Path $OutputDirectory).Path
$exe=Join-Path $directory 'PhoneDeck.exe'
$build=Get-Content (Join-Path $directory 'build.json') -Raw|ConvertFrom-Json
if((Get-FileHash $exe).Hash -ne $build.ExecutableHash){throw 'Artifact hash differs from build manifest'}
$results=@()
$recordPath=Join-Path $directory ('build-'+($build.Version.Split('.')[-1])+'.local.json')
$record=[ordered]@{Product='PhoneDeck';Version=$build.Version;Build=[int]$build.Version.Split('.')[-1];State='testing';ExecutableHash=$build.ExecutableHash;Sources=$build.Sources;Tests=$results;CallsTested=$false;RecordingImplemented=$true;RecordingTestAudio='synthetic';NativeAudioBackend=$true;AudioImplemented=$false;AudioValidated=$false}
$shell=Join-Path $env:WINDIR 'System32\WindowsPowerShell\v1.0\powershell.exe'
try{
 foreach($name in @('Test-Window','Test-CallHistory','Test-HistoryScroll','Test-ScrollChrome','Test-CallDetails','Test-HfpProtocol','Test-SystemHfp','Test-VoiceSettings','Test-Recording','Test-RecordingUi','Test-UlanziBridge','Test-CallReadiness','Test-LocalSetup')){
  & $shell -NoProfile -STA -ExecutionPolicy Bypass -File (Join-Path $root ('tests\'+$name+'.ps1')) -Executable $exe
  if($LASTEXITCODE){throw ($name+' failed')}
  $results+=@{Name=$name;Result='passed'}
 }
 & $shell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root 'tests\Test-DiagnosticsBuild.ps1')
 if($LASTEXITCODE){throw 'Test-DiagnosticsBuild failed'}
 $results+=@{Name='Test-DiagnosticsBuild';Result='passed'}
 # The installer fixture consumes the same tested artifact and manifest.
 $record.State='tested';$record.Tests=$results
 $record|ConvertTo-Json -Depth 8|Set-Content -LiteralPath $recordPath -Encoding UTF8
 & $shell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root 'tests\Test-Install.ps1') -Executable $exe
 if($LASTEXITCODE){throw 'Test-Install failed'}
 $results+=@{Name='Test-Install';Result='passed'}
 $record.State='tested';$record.Tests=$results
 'PASS: exact artifact verified without Bluetooth, real calls or audio changes.'
}catch{$record.State='failed';$record['Error']=$_.Exception.Message;throw}
finally{$record|ConvertTo-Json -Depth 8|Set-Content -LiteralPath $recordPath -Encoding UTF8}
