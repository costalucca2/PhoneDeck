$ErrorActionPreference='Stop'
Add-Type -AssemblyName UIAutomationClient,UIAutomationTypes
$a=[Reflection.Assembly]::LoadFrom((Join-Path $PSScriptRoot '..\out\PhoneDeck.exe'))
$t=$a.GetType('PhoneDirectDial')
$exe='C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe'
$script=Join-Path $PSScriptRoot 'Janela-Teste-Acionamento.ps1'
Remove-Item (Join-Path $PSScriptRoot 'teste-hwnd.local.txt') -ErrorAction SilentlyContinue
$p=Start-Process $exe -WindowStyle Hidden -ArgumentList ('-STA -NoProfile -ExecutionPolicy Bypass -File "'+$script+'"') -PassThru
try{
 for($i=0;$i -lt 30;$i++){Start-Sleep -Milliseconds 100; $p.Refresh();if(Test-Path (Join-Path $PSScriptRoot 'teste-hwnd.local.txt')){break}}
 $w=[Windows.Automation.AutomationElement]::FromHandle([IntPtr]([long](Get-Content (Join-Path $PSScriptRoot 'teste-hwnd.local.txt'))))
 [void]$t.GetMethod('ShowWindow',[Reflection.BindingFlags]'NonPublic,Static').Invoke($null,[object[]]@([IntPtr]([long](Get-Content (Join-Path $PSScriptRoot 'teste-hwnd.local.txt'))),[int]9))
 Start-Sleep -Milliseconds 300
 $b=$w.FindFirst([Windows.Automation.TreeScope]::Descendants,(New-Object Windows.Automation.PropertyCondition([Windows.Automation.AutomationElement]::NameProperty,'12345678')))
 $m=$t.GetMethod('PressCall',[Reflection.BindingFlags]'NonPublic,Static')
 [void](New-Object -ComObject WScript.Shell).AppActivate($p.Id)
 Start-Sleep -Milliseconds 250
 $m.Invoke($null,[object[]]@($w,$b,$b,$null,'12345678'))
 Start-Sleep -Milliseconds 400
 if((Get-Content (Join-Path $PSScriptRoot 'teste-cliques.local.txt')) -ne '1'){throw 'Expected one click'}
 try{$m.Invoke($null,[object[]]@($w,$b,$b,$null,'87654321'));throw 'Wrong number allowed'}catch {if($_.Exception.Message -like '*Wrong number allowed*'){throw}; 'Wrong number blocked'}
 if((Get-Content (Join-Path $PSScriptRoot 'teste-cliques.local.txt')) -ne '1'){throw 'Unexpected click'}
 'PASS: one native mouse click in isolated local test window; wrong number blocked; no telephone call.'
}finally{if(-not $p.HasExited){[void]$p.CloseMainWindow()}}





