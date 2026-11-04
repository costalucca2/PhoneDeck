param([string]$Executable=(Join-Path $PSScriptRoot '..\out\PhoneDeck.exe'))
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Windows.Forms,System.Drawing
$testRoot=Join-Path ([IO.Path]::GetTempPath()) ('phonedeck-test-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory (Join-Path $testRoot 'telefones') -Force|Out-Null
[IO.File]::WriteAllText((Join-Path $testRoot 'telefones\alpha.txt'),'00000000')
[IO.File]::WriteAllText((Join-Path $testRoot 'telefones\beta.txt'),'00000001')
[IO.File]::WriteAllText((Join-Path $testRoot 'telefones-contatos.json'),'[{"Id":"alpha","Name":"Ana teste"},{"Id":"beta","Name":"Bia teste"}]')
[AppDomain]::CurrentDomain.SetData('APPBASE',$testRoot)
$a=[Reflection.Assembly]::LoadFrom((Resolve-Path $Executable).Path)
$type=$a.GetType('PhoneDeckWindow')
$form=[Activator]::CreateInstance($type,[object[]]@($false))
$flags=[Reflection.BindingFlags]'Instance,NonPublic'
function Field($name){return ,($type.GetField($name,$flags).GetValue($form))}
function Assert($ok,$message){if(-not $ok){throw $message}}
try {
 $list=Field 'list'; $search=Field 'search'
 Assert ($list.Items.Count -eq 2) 'Fixture contacts failed'
 $search.Text='Bia'; Assert ($list.Items.Count -eq 1 -and $list.SelectedItem.ToString() -eq 'Bia teste') 'Name filter failed'
 $search.Text='00000000'; Assert ($list.Items.Count -eq 1) 'Phone filter failed'
 $search.Text=''; [void](Field 'favorites').Add('alpha')
 $type.GetMethod('SetTab',$flags).Invoke($form,[object[]]@($true))
 Assert ($list.Items.Count -eq 1 -and $list.SelectedItem.ToString() -eq 'Ana teste') 'Favorites filter failed'
 $type.GetMethod('SaveFavorites',$flags).Invoke($form,@())
 (Field 'favorites').Clear()
 $type.GetMethod('LoadFavorites',$flags).Invoke($form,@())
 Assert ((Field 'favorites').Contains('alpha')) 'Favorite persistence failed'
 $type.GetMethod('SetTab',$flags).Invoke($form,[object[]]@($false))
 $type.GetMethod('RenderMic',$flags).Invoke($form,[object[]]@('Microfone: silenciado'))
 Assert ((Field 'micState').Text.Contains('silenciado') -and (Field 'mic').Text -eq 'Ativar microfone') 'Muted display failed'
 $type.GetMethod('RenderMic',$flags).Invoke($form,[object[]]@('Microfone: ligado'))
 Assert ((Field 'micState').Text.Contains('ligado') -and (Field 'mic').Text -eq 'Silenciar microfone') 'Unmuted display failed'
 function Walk($control){$control;foreach($child in $control.Controls){Walk $child}}
 $buttons=@(Walk $form | Where-Object {$_ -is [Windows.Forms.Button]})
 foreach($key in @('7','0','⌫')){$button=$buttons|Where-Object {$_.Text -eq $key}|Select-Object -First 1;Assert ($null -ne $button) ('Key missing '+$key);$button.GetType().GetMethod('OnClick',$flags).Invoke($button,[object[]]@([EventArgs]::Empty))}
 Assert ((Field 'number').Text -eq '7') 'Dialpad append/backspace failed'
 Assert (@($buttons|Where-Object {$_.Text -in 'Ligar','Atender','Desligar'}).Count -eq 3) 'Call controls missing'
 'PASS: contacts, name/phone search, favorites, two microphone visual states, numeric keypad/backspace and three call controls. No real calls or audio changes.'
}finally{
 $form.Dispose()
 $resolved=[IO.Path]::GetFullPath($testRoot)
 $tempBase=[IO.Path]::GetFullPath([IO.Path]::GetTempPath())
 if($resolved.StartsWith($tempBase,[StringComparison]::OrdinalIgnoreCase) -and (Split-Path $resolved -Leaf).StartsWith('phonedeck-test-')){Remove-Item -LiteralPath $resolved -Recurse -Force}
}


