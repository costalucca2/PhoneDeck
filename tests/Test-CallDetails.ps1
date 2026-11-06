param([string]$Executable=(Join-Path $PSScriptRoot '..\out\PhoneDeck.exe'))
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Windows.Forms
[void][Reflection.Assembly]::LoadFrom((Resolve-Path $Executable).Path)
$folder=Join-Path ([IO.Path]::GetTempPath()) ('phonedeck-detail-test-'+[guid]::NewGuid().ToString('N'))
function Assert($ok,$text){if(-not $ok){throw $text}}
try{
 $id=[guid]::NewGuid().ToString('N');$other=[guid]::NewGuid().ToString('N')
 $row=[CallHistoryRow]::new();$row.SessionId=$id;$row.Contact='Synthetic contact';$row.Number='0211100000000';$row.RequestedAt=[DateTimeOffset]::Now;$row.Action='dial';$row.Journal=Join-Path $folder ('chamadas/'+$id+'/eventos.jsonl')
 $journal=[EvidenceJournal]::new($row.Journal);$journal.Append('call_command_requested',@{Action='dial';Context=@{CallSessionId=$id;NumberDialed=$row.Number;ContactName=$row.Contact}})
 foreach($entry in @(@('conversa-first',$id),@('conversa-second',$id),@('conversa-unrelated',$other))){
  $dir=Join-Path $folder $entry[0];New-Item -ItemType Directory -Path $dir -Force|Out-Null
  $context=New-Object 'System.Collections.Generic.Dictionary[string,object]';$context.Add('CallSessionId',$entry[1]);$context.Add('NumberDialed',$row.Number);$context.Add('ContactName',$row.Contact)
  $e=[RecordingEvidence]::new($dir,[guid]::NewGuid().ToString('N'),$context);$audio=Join-Path $dir 'conversa.wav';[IO.File]::WriteAllBytes($audio,[byte[]](82,73,70,70));$e.Finish($audio,'synthetic partial sample',0,@{Synthetic=$true})
 }
 $found=[CallRecordingLookup]::Find($folder,$row);Assert ($found.Count -eq 2) 'Session association failed or unrelated call included'
 Assert ($found[0].Integrity -and $found[0].State -eq 'partial') 'Partial bundle lost integrity/status'
 $unknown=[CallHistoryRow]::new();$unknown.Number=$row.Number;Assert (([CallRecordingLookup]::Find($folder,$unknown)).Count -eq 0) 'Number-only guessing used'
 $a=[Reflection.Assembly]::LoadFrom((Resolve-Path $Executable).Path);$type=$a.GetType('CallHistoryDetails');$flags=[Reflection.BindingFlags]'NonPublic,Instance'
 $form=$type.GetConstructors()[0].Invoke([object[]]@($row.PSObject.BaseObject,[string]$folder))
 try{
  Assert ($form.FormBorderStyle -eq 'None' -and $form.Controls.Find('DeckDialogTitleBar',$true)[0].Height -le 36) 'Native white titlebar remains'
  $task=$type.GetMethod('LoadRecords').Invoke($form,@());$until=[DateTime]::UtcNow.AddSeconds(8);while(-not $task.IsCompleted -and [DateTime]::UtcNow -lt $until){[Windows.Forms.Application]::DoEvents();Start-Sleep -Milliseconds 10};Assert ($task.IsCompleted) 'Detail load timed out';$task.GetAwaiter().GetResult()
  $list=$type.GetField('recordings',$flags).GetValue($form);$log=$type.GetField('log',$flags).GetValue($form)
  Assert ($list.Items.Count -eq 2 -and $log.Text.Contains('0211100000000') -and $log.Text.Contains('Solicitado em:')) 'Details missing log, exact number or recording time'
  $form.Show();[Windows.Forms.Application]::DoEvents();$form.PerformLayout();$bitmap=[Drawing.Bitmap]::new($form.Width,$form.Height);try{$form.DrawToBitmap($bitmap,[Drawing.Rectangle]::new(0,0,$form.Width,$form.Height));$bitmap.Save((Join-Path (Split-Path (Resolve-Path $Executable).Path) 'call-details-themed.local.png'))}finally{$bitmap.Dispose()}
 }finally{$form.Dispose()}
 [IO.File]::AppendAllText((Join-Path $found[0].Folder 'conversa.wav'),'tampered');Assert (-not ([CallRecordingLookup]::Find($folder,$row))[0].Integrity) 'Changed recording accepted'
 $empty=$type.GetConstructors()[0].Invoke([object[]]@($unknown.PSObject.BaseObject,[string]$folder))
 try{$task=$type.GetMethod('LoadRecords').Invoke($empty,@());while(-not $task.IsCompleted){[Windows.Forms.Application]::DoEvents();Start-Sleep -Milliseconds 10};$task.GetAwaiter().GetResult();Assert (-not $type.GetField('play',$flags).GetValue($empty).Enabled) 'Playback enabled without recording'}finally{$empty.Dispose()}
 'PASS: exact session association, multiple recordings, no number-only guesses, partial/tampered bundles and details with number/timestamps/log. No real audio played or calls made.'
}finally{if(Test-Path $folder){$resolved=[IO.Path]::GetFullPath($folder);if((Split-Path $resolved -Parent).TrimEnd('\') -eq ([IO.Path]::GetTempPath()).TrimEnd('\') -and (Split-Path $resolved -Leaf) -like 'phonedeck-detail-test-*'){Remove-Item -LiteralPath $resolved -Recurse -Force}}}