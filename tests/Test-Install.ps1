param([string]$Executable=(Join-Path $PSScriptRoot '..\out\PhoneDeck.exe'))
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$binary=(Resolve-Path $Executable).Path
if((Split-Path $binary -Parent) -ne (Resolve-Path (Join-Path $root 'out')).Path){throw 'Installer fixture expects default out directory'}
$tempBase=[IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$fixture=Join-Path $tempBase ('phonedeck-install-test-'+[guid]::NewGuid().ToString('N'))
$runtime=Join-Path $fixture 'runtime'
function Assert($value,$message){if(-not $value){throw $message}}
try{
 & (Join-Path $root 'scripts\install.ps1') -Runtime $runtime
 $hash=(Get-FileHash $binary).Hash
 foreach($alias in @('PhoneDeck.exe','LigarTerra.exe')){Assert ((Get-FileHash (Join-Path $runtime $alias)).Hash -eq $hash) ('Fresh alias differs: '+$alias)}
 Assert (-not (Test-Path (Join-Path $runtime 'ligar-terra-anterior'))) 'Fresh install created unnecessary previous release'
 $contact=Join-Path $runtime 'telefones\fixture.txt';[IO.File]::WriteAllText($contact,'00000000')
 $receiptPath=Join-Path $runtime 'ligar-terra-instalacao.json'
 $receipt=Get-Content $receiptPath -Raw|ConvertFrom-Json
 Assert ($receipt.AudioImplemented -eq $false -and $receipt.Channel -eq 'experimental') 'Receipt overstates audio availability'
 $receipt.Version='0.0.0.0';$receipt.ExecutableHash='old-fixture'
 $receipt|ConvertTo-Json -Depth 12|Set-Content $receiptPath -Encoding UTF8
 foreach($alias in @('PhoneDeck.exe','LigarTerra.exe')){[IO.File]::WriteAllText((Join-Path $runtime $alias),'old-fixture-binary')}
 $oldHash=(Get-FileHash (Join-Path $runtime 'PhoneDeck.exe')).Hash
 & (Join-Path $root 'scripts\install.ps1') -Runtime $runtime
 $previous=Join-Path $runtime 'ligar-terra-anterior'
 Assert ((Get-FileHash (Join-Path $previous 'PhoneDeck.exe')).Hash -eq $oldHash) 'Immediate previous was not preserved'
 Assert ([IO.File]::ReadAllText($contact) -eq '00000000') 'Private contact changed'
 & (Join-Path $root 'scripts\install.ps1') -Runtime $runtime
 Assert ((Get-FileHash (Join-Path $previous 'PhoneDeck.exe')).Hash -eq $oldHash) 'Idempotent install replaced previous backup'
 Assert (@(Get-ChildItem $runtime -Directory|Where-Object Name -like 'previous-*.local').Count -eq 0) 'Temporary release backup remained'
 'PASS: clean install, both aliases, experimental audio status, upgrade, contact preservation, single previous release and idempotence. No executable or phone action launched.'
}finally{
 $resolved=[IO.Path]::GetFullPath($fixture)
 if($resolved.StartsWith($tempBase,[StringComparison]::OrdinalIgnoreCase) -and (Split-Path $resolved -Leaf).StartsWith('phonedeck-install-test-') -and (Test-Path -LiteralPath $resolved)){
  if(@(Get-ChildItem $resolved -Recurse -Force|Where-Object {$_.Attributes -band [IO.FileAttributes]::ReparsePoint}).Count){throw 'Fixture contains junctions; cleanup refused'}
  Remove-Item -LiteralPath $resolved -Recurse -Force
 }
}
