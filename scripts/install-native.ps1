param(
 [Parameter(Mandatory=$true)][string]$SdkTools,
 [Parameter(Mandatory=$true)][string]$CertificateThumbprint,
 [string]$PackageVersion='0.2.0.46'
)
# Run in Windows PowerShell 5.1. Uses an existing trusted PhoneDeck signing identity.
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$out=Join-Path $root 'out'
$exe=Join-Path $out 'PhoneDeck.exe'
$build=Get-Content (Join-Path $out 'build.json') -Raw|ConvertFrom-Json
$record=Get-Content (Join-Path $out ('build-'+$build.Version.Split('.')[-1]+'.local.json')) -Raw|ConvertFrom-Json
$hash=(Get-FileHash $exe).Hash
if($record.State -notin @('tested','verified') -or $record.ExecutableHash -ne $hash -or $build.ExecutableHash -ne $hash){throw 'Run build.ps1 and verify.ps1 on this exact artifact first'}
$lock=[IO.File]::Open((Join-Path $out 'native-install.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
try {
 $installed=Get-AppxPackage PhoneDeck.NativeBridge
 if($installed -and $installed.Version -eq [version]$PackageVersion){
  if((Get-FileHash (Join-Path $installed.InstallLocation 'NativeProbe.exe')).Hash -ne $hash){throw 'Same package version has different code; reserve a new component version'}
  'Native package already verified '+$PackageVersion;return
 }
 if($installed -and $installed.Version -gt [version]$PackageVersion){throw 'Native package version must not go backwards'}
 if(Get-Process NativeProbe -ErrorAction SilentlyContinue){throw 'While phone is idle, run PhoneDeck.exe --native-stop before updating the native package'}
 $cert=Get-Item ('Cert:\CurrentUser\My\'+$CertificateThumbprint)
 if($cert.Subject -ne 'CN=PhoneDeck' -or -not $cert.HasPrivateKey){throw 'Expected an existing CN=PhoneDeck certificate with private key'}
 foreach($name in 'makeappx.exe','signtool.exe'){
  if((Get-AuthenticodeSignature (Join-Path $SdkTools $name)).Status -ne 'Valid'){throw 'Invalid Windows SDK tool signature'}
 }
 $work=Join-Path $out 'native-package'
 $stage=Join-Path $work 'stage'
 New-Item -ItemType Directory -Path $stage -Force|Out-Null
 Copy-Item -LiteralPath $exe -Destination (Join-Path $stage 'NativeProbe.exe') -Force
 Copy-Item -LiteralPath (Join-Path $root 'assets\PhoneDeck.png') -Destination (Join-Path $stage 'PhoneDeck.png') -Force
 $manifest=@"
<?xml version="1.0" encoding="utf-8"?>
<Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10" xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10" xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities" IgnorableNamespaces="uap rescap">
 <Identity Name="PhoneDeck.NativeBridge" Publisher="CN=PhoneDeck" Version="$PackageVersion" ProcessorArchitecture="x64" />
 <Properties><DisplayName>PhoneDeck Bluetooth</DisplayName><PublisherDisplayName>PhoneDeck</PublisherDisplayName><Logo>PhoneDeck.png</Logo></Properties>
 <Resources><Resource Language="pt-br" /></Resources>
 <Dependencies><TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.19041.0" MaxVersionTested="10.0.19045.0" /></Dependencies>
 <Applications><Application Id="NativeProbe" Executable="NativeProbe.exe" EntryPoint="Windows.FullTrustApplication"><uap:VisualElements DisplayName="PhoneDeck Bluetooth" Description="Controle e preparacao de voz Bluetooth" BackgroundColor="transparent" Square150x150Logo="PhoneDeck.png" Square44x44Logo="PhoneDeck.png" /></Application></Applications>
 <Capabilities><uap:Capability Name="phoneCall" /><rescap:Capability Name="runFullTrust" /><rescap:Capability Name="phoneLineTransportManagement" /><DeviceCapability Name="bluetooth" /></Capabilities>
</Package>
"@
 [IO.File]::WriteAllText((Join-Path $stage 'AppxManifest.xml'),$manifest,[Text.UTF8Encoding]::new($false))
 $candidate=Join-Path $work 'candidate.msix'
 & (Join-Path $SdkTools 'makeappx.exe') pack /o /d $stage /p $candidate
 if($LASTEXITCODE){throw 'Native package build failed'}
 & (Join-Path $SdkTools 'signtool.exe') sign /fd SHA256 /s My /sha1 $cert.Thumbprint $candidate
 if($LASTEXITCODE){throw 'Native package signing failed'}
 Add-AppxPackage -Path $candidate
 $actual=Get-AppxPackage PhoneDeck.NativeBridge
 if(-not $actual -or $actual.Status -ne 'Ok' -or $actual.Version -ne [version]$PackageVersion -or (Get-FileHash (Join-Path $actual.InstallLocation 'NativeProbe.exe')).Hash -ne $hash){throw 'Installed native artifact differs'}
 $current=Join-Path $work 'current.msix'
 $previous=Join-Path $work 'previous.msix'
 if(Test-Path $current){Copy-Item -LiteralPath $current -Destination $previous -Force}
 Move-Item -LiteralPath $candidate -Destination $current -Force
 [pscustomobject]@{Product='PhoneDeck.NativeBridge';Version=$PackageVersion;State='verified';ExecutableHash=$hash;PackageHash=(Get-FileHash $current).Hash;CallsPerformed=$false;VoiceValidated=$false}|ConvertTo-Json|Set-Content (Join-Path $work 'installation.local.json') -Encoding UTF8
 'Native package verified '+$PackageVersion+'. No calls performed.'
}finally{$lock.Dispose()}
