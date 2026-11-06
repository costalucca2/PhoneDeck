param([string]$Executable=(Join-Path $PSScriptRoot '..\out\PhoneDeck.exe'))
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Windows.Forms,System.Drawing
$prefRoot=Join-Path ([IO.Path]::GetTempPath()) ('phonedeck-preferences-'+[guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($prefRoot)
[AppDomain]::CurrentDomain.SetData('APPBASE',$prefRoot)
$a=[Reflection.Assembly]::LoadFrom((Resolve-Path $Executable).Path)
$form=[Activator]::CreateInstance($a.GetType('AudioSettingsForm'),[object[]]@($true))
try{
 function Find($name){$found=$form.Controls.Find($name,$true);if($found.Count -ne 1){throw ('Missing setting '+$name)};return $found[0]}
 $input=Find 'CallMicrophone';$output=Find 'CallSpeaker';$level=Find 'MicrophoneLevel'
 if($input.SelectedItem.Name -ne 'Microfone de teste' -or $output.SelectedItem.Name -ne 'Caixinha de teste'){throw 'Fixture devices not selected'}
 if($level.Value -ne 42){throw 'Existing microphone level lost'}
 $level.Value=67
 foreach($name in 'MicrophoneMuted','MicrophoneMeter','CheckMicrophone','NoiseProcessor','ApplyAudioSettings','AudioSettingsStatus'){[void](Find $name)}
 if((Find 'ApplyAudioSettings').Enabled -or (Find 'CheckMicrophone').Enabled -or (Find 'NoiseProcessor').Enabled){throw 'Fixture can modify devices or start capture'}
 [void]$form.Handle
 $form.Show();[Windows.Forms.Application]::DoEvents()
 $iconBitmap=New-Object Drawing.Bitmap(40,40)
 $iconGraphics=[Drawing.Graphics]::FromImage($iconBitmap)
 try{foreach($icon in 'sun','moon','settings','record','stop','close'){$a.GetType('VectorIcons').GetMethod('Draw').Invoke($null,[object[]]@($iconGraphics,$icon,[Drawing.Rectangle]::new(2,2,32,32),[Drawing.Color]::Navy))}}finally{$iconGraphics.Dispose();$iconBitmap.Dispose()}
 if($input.Width -lt 450 -or $output.Width -lt 450){throw "Voice controls collapsed instead of filling the content area"}
 $body=$a.GetType('AudioSettingsForm').GetField('body',[Reflection.BindingFlags]'NonPublic,Instance').GetValue($form)
 $rows=@($body.Controls|Sort-Object Top)
 for($i=1;$i -lt $rows.Count;$i++){if($rows[$i].Top -lt $rows[$i-1].Bottom){throw 'Voice settings rows overlap'}}
 if($body.HorizontalScroll.Visible){throw 'Unexpected horizontal scrolling in voice settings'}
 $bitmap=New-Object Drawing.Bitmap($form.Width,$form.Height)
 try {
  $form.DrawToBitmap($bitmap,[Drawing.Rectangle]::new(0,0,$form.Width,$form.Height))
  $bitmap.Save((Join-Path (Split-Path (Resolve-Path $Executable).Path -Parent) 'voice-settings-fixture.local.png'))
 }finally{$bitmap.Dispose()}
 $preferences=$a.GetType('UiPreferences')
 $set=$preferences.GetMethod('Set',[Reflection.BindingFlags]'Static,Public')
 $set.Invoke($null,[object[]]@($true,$true,$true))
 [Windows.Forms.Application]::DoEvents()
 if($input.SelectedItem.Id -ne 'fixture-in' -or $level.Value -ne 67){throw 'Theme/language changed audio selection or gain'}
 $saved=Get-Content (Join-Path $prefRoot 'preferences.local.json') -Raw|ConvertFrom-Json
 if($saved.'on-tema' -ne 'claro' -or $saved.'on-idioma' -ne 'en'){throw 'Preferences not persisted'}
 if($form.Text -notmatch 'Voice'){throw 'Settings title not translated'}
 $picture=New-Object Drawing.Bitmap($form.Width,$form.Height)
 try{$form.DrawToBitmap($picture,[Drawing.Rectangle]::new(0,0,$form.Width,$form.Height));$picture.Save((Join-Path (Split-Path (Resolve-Path $Executable).Path -Parent) 'voice-settings-light-en.local.png'))}finally{$picture.Dispose()}
 $set.Invoke($null,[object[]]@($false,$false,$false))
 if($form.Text -notmatch 'Voz'){throw 'Portuguese restore failed'}
 $preferenceFile=Join-Path $prefRoot 'preferences.local.json'
 [IO.File]::WriteAllText($preferenceFile,'{"on-tema":"escuro","on-idioma":"pt"}')
 [IO.File]::SetLastWriteTimeUtc($preferenceFile,[DateTime]::UtcNow.AddSeconds(2))
 $preferences.GetMethod('Reload',[Reflection.BindingFlags]'NonPublic,Static').Invoke($null,@())
 if($preferences.GetProperty('Light').GetValue($null) -or $preferences.GetProperty('English').GetValue($null)){throw 'External preference changes were ignored'}
 'PASS: voice settings devices, level, mute, meter and processor entry; fixture cannot access devices, microphone capture or calls.'
}finally{$form.Dispose();if(Test-Path $prefRoot){$resolved=[IO.Path]::GetFullPath($prefRoot);if((Split-Path $resolved -Parent).TrimEnd('\') -eq ([IO.Path]::GetTempPath()).TrimEnd('\') -and (Split-Path $resolved -Leaf) -like 'phonedeck-preferences-*'){Remove-Item -LiteralPath $resolved -Recurse -Force}}}
