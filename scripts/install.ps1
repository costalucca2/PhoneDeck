param([string]$Runtime=(Join-Path $env:LOCALAPPDATA 'PhoneDeck'))
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$runtimePath=[IO.Path]::GetFullPath($Runtime).TrimEnd('\')
if($runtimePath -eq [IO.Path]::GetPathRoot($runtimePath).TrimEnd('\')){throw 'Do not install in a drive root'}
$artifact=Join-Path $root 'out\PhoneDeck.exe'
$build=Get-Content (Join-Path $root 'out\build.json') -Raw|ConvertFrom-Json
$recordPath=Join-Path $root ('out\build-'+($build.Version.Split('.')[-1])+'.local.json')
$record=Get-Content -LiteralPath $recordPath -Raw|ConvertFrom-Json
if($record.State -notin @('tested','verified') -or $record.ExecutableHash -ne $build.ExecutableHash){throw 'This exact artifact has not passed tests; run scripts/verify.ps1'}
if((Get-FileHash $artifact).Hash -ne $build.ExecutableHash){throw 'Artifact hash mismatch'}
foreach($entry in $build.Sources){if((Get-FileHash (Join-Path $root ('src\'+$entry.File))).Hash -ne $entry.Hash){throw 'Source changed after build'}}
foreach($entry in $build.Assets){if((Get-FileHash (Join-Path $root ('assets\'+$entry.File))).Hash -ne $entry.Hash){throw 'SVG asset changed after build'}}
if(-not (Test-Path -LiteralPath $runtimePath)){New-Item -ItemType Directory -Path $runtimePath|Out-Null}
if((Get-Item -LiteralPath $runtimePath).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Runtime must not be a junction'}
$lock=[IO.File]::Open((Join-Path $runtimePath 'ligar-terra-install.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
$stage=Join-Path $runtimePath ('previous-'+$build.Version+'.local')
$previous=Join-Path $runtimePath 'ligar-terra-anterior'
$receiptPath=Join-Path $runtimePath 'ligar-terra-instalacao.json'
$aliases=@('PhoneDeck.exe','LigarTerra.exe')
$priorExisting=@();$copied=@();$promoted=$false;$receiptWritten=$false;$recordingLock=$null;$recordingHeld=$false
function Write-Json($path,$value){
 $temp=$path+'.local.tmp'
 [IO.File]::WriteAllText($temp,($value|ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false))
 if(Test-Path -LiteralPath $path){[IO.File]::Replace($temp,$path,[NullString]::Value)}else{[IO.File]::Move($temp,$path)}
}
function Remove-VerifiedBackup($path){
 $resolved=[IO.Path]::GetFullPath($path)
 if((Split-Path $resolved -Parent) -ne $runtimePath -or (Split-Path $resolved -Leaf) -notmatch '^(ligar-terra-anterior|previous-[0-9.]+\.local)$'){throw 'Unsafe backup cleanup path'}
 if(Test-Path -LiteralPath $resolved){
  if((Get-Item -LiteralPath $resolved).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Backup is a junction'}
  if(@(Get-ChildItem -LiteralPath $resolved -Recurse -Force|Where-Object {$_.Attributes -band [IO.FileAttributes]::ReparsePoint}).Count){throw 'Backup contains junctions'}
  Remove-Item -LiteralPath $resolved -Recurse -Force
 }
}
try{
 if(Test-Path -LiteralPath $stage){throw 'Previous install attempt exists; inspect it before proceeding'}
 if((Test-Path -LiteralPath $receiptPath) -and @($aliases|Where-Object {-not (Test-Path -LiteralPath (Join-Path $runtimePath $_)) -or (Get-FileHash (Join-Path $runtimePath $_)).Hash -ne $build.ExecutableHash}).Count -eq 0){
  $installed=Get-Content -LiteralPath $receiptPath -Raw|ConvertFrom-Json
  if($installed.Version -eq $build.Version -and $installed.ExecutableHash -eq $build.ExecutableHash){'Already verified '+$build.Version+'; backup preserved';return}
 }
 $targets=@($aliases|ForEach-Object {Join-Path $runtimePath $_})
 $processes=@(Get-Process PhoneDeck,LigarTerra -ErrorAction SilentlyContinue|Where-Object {$_.Path -and $targets -contains $_.Path})
 if($processes.Count){$recordingLock=New-Object Threading.Mutex($false,'Local\PhoneDeck.ConversationRecording');try{$recordingHeld=$recordingLock.WaitOne(0)}catch [Threading.AbandonedMutexException]{$recordingHeld=$true};if(-not $recordingHeld){throw 'Conversation recording active. Stop and save it before updating PhoneDeck'}}
 foreach($hidden in @($processes|Where-Object {$_.MainWindowHandle -eq 0})){
  $details=Get-CimInstance Win32_Process -Filter ('ProcessId='+$hidden.Id)
  if($details.CommandLine -notmatch '(?:^|\s)--record-panel(?:\s|$)'){throw 'PhoneDeck background process running. While phone is idle, run PhoneDeck.exe --native-stop, then retry installation'}
  if(-not ('PhoneDeckInstallerWindows' -as [type])){
   Add-Type @"
using System;using System.Runtime.InteropServices;
public static class PhoneDeckInstallerWindows {
 delegate bool Callback(IntPtr window,IntPtr data);
 [DllImport("user32.dll")]static extern bool EnumWindows(Callback callback,IntPtr data);
 [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr window,out uint pid);
 [DllImport("user32.dll")]static extern bool IsWindowVisible(IntPtr window);
 [DllImport("user32.dll")]static extern bool PostMessage(IntPtr window,uint message,IntPtr w,IntPtr l);
 public static void Close(uint owner){EnumWindows((window,data)=>{uint pid;GetWindowThreadProcessId(window,out pid);if(pid==owner&&IsWindowVisible(window))PostMessage(window,0x10,IntPtr.Zero,IntPtr.Zero);return true;},IntPtr.Zero);}
}
"@
  }
  [PhoneDeckInstallerWindows]::Close([uint32]$hidden.Id)
  if(-not $hidden.WaitForExit(3000)){throw 'Idle recorder panel did not close; executable preserved'}
 }
 $processes=@($processes|Where-Object {-not $_.HasExited})
 foreach($process in $processes){[void]$process.CloseMainWindow();if(-not $process.WaitForExit(3000)){$process.Kill();$process.WaitForExit()}}
 New-Item -ItemType Directory -Path $stage|Out-Null
 foreach($name in @($aliases)+@('ligar-terra-instalacao.json')){if(Test-Path -LiteralPath (Join-Path $runtimePath $name)){Copy-Item -LiteralPath (Join-Path $runtimePath $name) -Destination (Join-Path $stage $name);$priorExisting+=$name}}
 foreach($name in $aliases){Copy-Item -LiteralPath $artifact -Destination (Join-Path $runtimePath $name) -Force;$copied+=$name}
 foreach($name in $aliases){if((Get-FileHash (Join-Path $runtimePath $name)).Hash -ne $build.ExecutableHash){throw 'Installed alias hash mismatch'}}
 $receipt=if(Test-Path -LiteralPath $receiptPath){Get-Content -LiteralPath $receiptPath -Raw|ConvertFrom-Json}else{[pscustomobject]@{}}
 $fields=@{Product='PhoneDeck';Version=$build.Version;ExecutableHash=$build.ExecutableHash;Sources=$build.Sources;Target='PhoneDeck HFP direct';Backend='Windows native HFP';NativeAudioBackend=$true;Channel='experimental';AudioImplemented=$false;AudioValidated=$false;CallsTested=$false;Test='Exact binary tested with simulated phone. Native voice preparation integrated; conversation not validated.'}
 foreach($key in $fields.Keys){$receipt|Add-Member -NotePropertyName $key -NotePropertyValue $fields[$key] -Force}
 Write-Json $receiptPath $receipt;$receiptWritten=$true
 $verified=Get-Content -LiteralPath $receiptPath -Raw|ConvertFrom-Json
 if($verified.Version -ne $build.Version -or $verified.ExecutableHash -ne $build.ExecutableHash){throw 'Installed receipt mismatch'}
 $contacts=Join-Path $runtimePath 'telefones';if(-not (Test-Path -LiteralPath $contacts)){New-Item -ItemType Directory -Path $contacts|Out-Null}
 $promoted=$true
 if($priorExisting.Count){try{Remove-VerifiedBackup $previous;Move-Item -LiteralPath $stage -Destination $previous}catch{Write-Warning ('Installed; backup rotation pending: '+$_.Exception.Message)}}else{Remove-VerifiedBackup $stage}
 $record.State='verified';$record|Add-Member -NotePropertyName InstalledAt -NotePropertyValue ([DateTimeOffset]::Now.ToString('o')) -Force
 Write-Json $recordPath $record
 'Verified experimental PhoneDeck '+$build.Version+' in both aliases. No calls executed; native voice backend integrated, conversation not validated.'
}catch{
 if(-not $promoted -and (Test-Path -LiteralPath $stage)){
  foreach($name in $priorExisting){Copy-Item -LiteralPath (Join-Path $stage $name) -Destination (Join-Path $runtimePath $name) -Force}
  foreach($name in $copied){if($name -notin $priorExisting){Remove-Item -LiteralPath (Join-Path $runtimePath $name) -Force}}
  if($receiptWritten -and 'ligar-terra-instalacao.json' -notin $priorExisting){Remove-Item -LiteralPath $receiptPath -Force}
 }
 throw
}finally{if($recordingHeld){$recordingLock.ReleaseMutex()};if($recordingLock){$recordingLock.Dispose()};$lock.Dispose()}
