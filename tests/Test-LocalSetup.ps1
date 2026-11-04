param([string]$Executable=(Join-Path $PSScriptRoot '..\out\PhoneDeck.exe'))
$ErrorActionPreference='Stop'
$a=[Reflection.Assembly]::LoadFrom((Resolve-Path $Executable).Path)
$a.GetType('EmbeddedDependencies').GetMethod('Load').Invoke($null,@())
$qr=[Reflection.Assembly]::Load('QRCoder')
if(-not $qr.GetType('QRCoder.QRCodeGenerator')){throw 'Embedded QR dependency missing'}
$type=$a.GetType('LocalSetupServer')
$server=[Activator]::CreateInstance($type,[object[]]@([Net.IPAddress]::Loopback,0))
$url=$type.GetField('Url').GetValue($server)
function Status($uri,$method){try{(Invoke-WebRequest $uri -Method $method -UseBasicParsing -TimeoutSec 4).StatusCode}catch{if($_.Exception.Response){[int]$_.Exception.Response.StatusCode}else{throw}}}
try{
 $r=Invoke-WebRequest $url -UseBasicParsing -TimeoutSec 4
 if($r.StatusCode -ne 200 -or $r.Content -notmatch 'iPhone' -or $r.Content -notmatch 'Android'){throw 'Local guide invalid'}
 if($r.Headers['Cache-Control'] -ne 'no-store'){throw 'Local guide cached'}
 if((Status ($url+'wrong') 'GET') -ne 404){throw 'Token boundary failed'}
 if((Status $url 'POST') -ne 405){throw 'Write request not blocked'}
 if(-not $type.GetField('GuideOpened').GetValue($server)){throw 'Guide open not detected'}
 'PASS: embedded QR library; local guide for iPhone/Android; token required; POST blocked; no cache. No Bluetooth or call actions.'
}finally{$server.Dispose()}
