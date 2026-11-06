param([string]$Executable=(Join-Path $PSScriptRoot '..\out\PhoneDeck.exe'))
$testOutput=Split-Path (Resolve-Path $Executable).Path -Parent
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Windows.Forms,System.Drawing
$a=[Reflection.Assembly]::LoadFrom((Join-Path $testOutput 'PhoneDeck.exe'))
$source=@"
using System;using System.Threading;
public static class RecorderUiFixture {public static ConversationRecorder Start(string folder){var r=new ConversationRecorder((id,flow,sink,ready,stop)=>{ready();while(!stop.IsCancellationRequested){var v=new float[480];for(int i=0;i<v.Length;i++)v[i]=.1f;sink(v,48000);Thread.Sleep(10);}});r.Start(folder,"synthetic microphone","synthetic output");return r;}}
"@
$cs=Join-Path $testOutput 'recording-ui-fixture.local.cs';$dll=Join-Path $testOutput 'recording-ui-fixture.local.dll'
[IO.File]::WriteAllText($cs,$source)
& (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe') /nologo /target:library ('/reference:'+(Join-Path $testOutput 'PhoneDeck.exe')) ('/out:'+$dll) $cs
if($LASTEXITCODE){throw 'Fixture compilation failed'}
[void][Reflection.Assembly]::LoadFrom($dll)
$folder=Join-Path ([IO.Path]::GetTempPath()) ('phonedeck-ui-recording-'+[guid]::NewGuid().ToString('N'))
$type=$a.GetType('RecordingPanel');$form=[Activator]::CreateInstance($type);$flags=[Reflection.BindingFlags]'NonPublic,Instance';$record=$null
try{
 $form.Show();[Windows.Forms.Application]::DoEvents();$button=$form.Controls.Find('ConversationRecordToggle',$true)[0]
 if($button.Text -ne 'Gravar'){throw 'Manual recorder starts in unexpected state'}
 $record=[RecorderUiFixture]::Start($folder);Start-Sleep -Milliseconds 80
 $type.GetField('recorder',$flags).SetValue($form,$record);$type.GetMethod('RefreshState',$flags).Invoke($form,@());[Windows.Forms.Application]::DoEvents()
 if($button.Text -ne 'Parar e salvar' -or $button.GetType().GetField('Fill').GetValue($button).R -ne 232){throw 'Active recording does not show red Stop'}
 $bitmap=New-Object Drawing.Bitmap($form.Width,$form.Height)
 try{$form.DrawToBitmap($bitmap,[Drawing.Rectangle]::new(0,0,$form.Width,$form.Height));$bitmap.Save((Join-Path $testOutput 'recording-red-fixture.local.png'))}finally{$bitmap.Dispose()}
 $record.Stop("manual_stop").GetAwaiter().GetResult();$type.GetMethod('Stop').Invoke($form,[object[]]@('manual_stop')).GetAwaiter().GetResult();[Windows.Forms.Application]::DoEvents()
 if($button.Text -ne 'Gravar' -or $button.GetType().GetField('Fill').GetValue($button).G -ne 186){throw 'Stopped recorder does not show green Record'}
 $bitmap=New-Object Drawing.Bitmap($form.Width,$form.Height)
 try{$form.DrawToBitmap($bitmap,[Drawing.Rectangle]::new(0,0,$form.Width,$form.Height));$bitmap.Save((Join-Path $testOutput 'recording-green-fixture.local.png'))}finally{$bitmap.Dispose()}
 'PASS: manual green Record, red Stop/save, timer and successful return to green. Synthetic samples only; no microphone/call captured.'
}finally{if($record -and $record.Recording){$record.Stop("manual_stop").GetAwaiter().GetResult()};$form.Close();$form.Dispose();if(Test-Path $folder){$resolved=[IO.Path]::GetFullPath($folder);if((Split-Path $resolved -Parent).TrimEnd('\') -eq ([IO.Path]::GetTempPath()).TrimEnd('\') -and (Split-Path $resolved -Leaf) -like 'phonedeck-ui-recording-*'){Remove-Item -LiteralPath $resolved -Recurse -Force}}}