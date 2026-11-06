param([string]$Executable=(Join-Path $PSScriptRoot '..\out\PhoneDeck.exe'))
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Windows.Forms
[void][Reflection.Assembly]::LoadFrom((Resolve-Path $Executable).Path)
$folder=Join-Path ([IO.Path]::GetTempPath()) ('phonedeck-history-test-'+[guid]::NewGuid().ToString('N'))
function Assert($ok,$text){if(-not $ok){throw $text}}
try{
 Assert (([CallHistory]::Read($folder)).Rows.Count -eq 0) 'Empty history failed'
 $file=Join-Path $folder 'chamadas/session/eventos.jsonl'
 $journal=[EvidenceJournal]::new($file)
 $ctx=@{ContactName='Synthetic contact';NumberDialed='0211100000000'}
 $journal.Append('call_command_requested',@{Action='dial';Context=$ctx})
 $journal.Append('call_command_confirmed',@{Action='dial';ConnectedConfirmed=$false})
 $journal.Append('call_command_requested',@{Action='hangup';Context=$ctx})
 $journal.Append('call_command_failed',@{Action='hangup';Error='Synthetic failure'})
 $r=[CallHistory]::Read($folder)
 Assert ($r.Rows.Count -eq 2) 'History rows missing'
 Assert (@($r.Rows|Where-Object {$_.Outcome -eq 'confirmed'}).Count -eq 1) 'Acknowledgement missing'
 Assert (@($r.Rows|Where-Object {$_.Outcome -eq 'failed'}).Count -eq 1) 'Failure missing'
 Assert ($r.Rows[0].Number -eq '0211100000000') 'Phone leading zero changed'
 $a=[Reflection.Assembly]::LoadFrom((Resolve-Path $Executable).Path);$t=$a.GetType('PhoneDeckWindow');$f=[Activator]::CreateInstance($t,[object[]]@($false));$flags=[Reflection.BindingFlags]'Instance,NonPublic'
 try{
  [void]$t.GetMethod('RenderHistory',$flags).Invoke($f,[object[]]@($r))
  $view=$t.GetField('recent',$flags).GetValue($f)
  Assert ($view.Items.Count -eq 2 -and $view.Columns.Count -eq 5) 'UI did not display local history'
  Assert (@($view.Items|Where-Object {$_.SubItems[4].Text -eq 'Comando aceito'}).Count -eq 1) 'UI falsely labels acknowledgement'
  [void]$t.GetMethod('RenderHistory',$flags).Invoke($f,[object[]]@([CallHistoryResult]::new()))
  Assert ($view.Items.Count -eq 0 -and $t.GetField('syncLabel',$flags).GetValue($f).Text.Contains('Nenhum comando')) 'Empty state missing'
 }finally{$f.Dispose()}
 $bytes=[IO.File]::ReadAllBytes($file);[IO.File]::WriteAllText($file,[IO.File]::ReadAllText($file).Replace('Synthetic contact','Changed contact'))
 $r=[CallHistory]::Read($folder);Assert ($r.Rows.Count -eq 0 -and $r.Unreadable -eq 1) 'Tampered journal displayed'
 [IO.File]::WriteAllBytes($file,$bytes);[IO.File]::AppendAllText($file,'{"incomplete":')
 $r=[CallHistory]::Read($folder);Assert ($r.Rows.Count -eq 2) 'In-progress trailing event hid completed commands'
 'PASS: local history, leading zeros, separate command outcomes, five UI columns, empty state, tamper rejection and in-progress append; no phone used.'
}finally{
 if(Test-Path $folder){$resolved=[IO.Path]::GetFullPath($folder);if((Split-Path $resolved -Parent).TrimEnd('\') -eq ([IO.Path]::GetTempPath()).TrimEnd('\') -and (Split-Path $resolved -Leaf) -like 'phonedeck-history-test-*'){Remove-Item -LiteralPath $resolved -Recurse -Force}}
}