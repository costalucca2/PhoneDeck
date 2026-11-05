param([string]$Executable=(Join-Path $PSScriptRoot '..\out\PhoneDeck.exe'))
$ErrorActionPreference='Stop'
Add-Type -AssemblyName UIAutomationClient,UIAutomationTypes,System.Windows.Forms,System.Drawing
$assembly=[Reflection.Assembly]::LoadFrom((Resolve-Path $Executable).Path)
$method=$assembly.GetType('PhoneSnapshot').GetMethod('CallReadiness',[Reflection.BindingFlags]'Static,Public')
$cases=@(@($true,$false,$false,'Unavailable'),@($true,$true,$false,'DialerAvailable'),@($true,$true,$true,'Error'),@($false,$true,$false,'Unverified'),@($false,$false,$false,'Unverified'))
foreach($case in $cases){$value=$method.Invoke($null,[object[]]@($case[0],$case[1],$case[2]));if($value -ne $case[3]){throw ('Incorrect call readiness: '+$value)}}
'PASS: unavailable setup never claims ready; settings view remains unverified; dialer presence differs from connected-call proof; explicit errors win. No device or call actions.'