$ErrorActionPreference = 'Stop'
$folder = Join-Path $PSScriptRoot 'certificado-produccion'
if (Test-Path -LiteralPath $folder) { throw 'Ya hay un certificado de producción preparado. No se sobrescribió.' }
$cert = [System.Security.Cryptography.X509Certificates.X509CertificateLoader]::LoadPkcs12FromFile('C:\Certificados\MIL-DAN\FactElectMilDan.p12', $null, ([System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::EphemeralKeySet -bor [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::Exportable))
if (-not $cert.HasPrivateKey -or $cert.Issuer -match 'Computadores Test' -or $cert.NotAfter -le (Get-Date)) { throw 'El certificado no es válido para producción.' }
$configPath = Join-Path $PSScriptRoot '../SistemaGestionFerreteria/appsettings.json'
$cfg = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
if ($cert.Subject -notmatch ('CUIT\s*' + [regex]::Escape($cfg.Afip.Cuit.Replace('-','')))) { throw 'El certificado no corresponde al CUIT configurado.' }
$random = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Fill($random)
$password = [Convert]::ToBase64String($random)
$bytes = $cert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Pfx, $password)
$check = [System.Security.Cryptography.X509Certificates.X509CertificateLoader]::LoadPkcs12($bytes, $password, [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::EphemeralKeySet)
$rsa = [System.Security.Cryptography.X509Certificates.RSACertificateExtensions]::GetRSAPrivateKey($check)
$pub = [System.Security.Cryptography.X509Certificates.RSACertificateExtensions]::GetRSAPublicKey($check)
$data = [Text.Encoding]::UTF8.GetBytes('Verificar certificado de produccion')
$signature = $rsa.SignData($data, [System.Security.Cryptography.HashAlgorithmName]::SHA256, [System.Security.Cryptography.RSASignaturePadding]::Pkcs1)
if (-not $pub.VerifyData($data, $signature, [System.Security.Cryptography.HashAlgorithmName]::SHA256, [System.Security.Cryptography.RSASignaturePadding]::Pkcs1)) { throw 'La prueba de firma falló.' }
# Perfil separado de homologación: no se carga por defecto al activar producción.
@{Afip=$cfg.Afip} | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath (Join-Path $PSScriptRoot '../SistemaGestionFerreteria/appsettings.Homologacion.json') -Encoding utf8
New-Item -ItemType Directory -Path $folder | Out-Null
$pfx = [IO.Path]::GetFullPath((Join-Path $folder 'produccion.pfx'))
$protected = [IO.Path]::GetFullPath((Join-Path $folder 'password-protegida.txt'))
[IO.File]::WriteAllBytes($pfx, $bytes)
ConvertTo-SecureString $password -AsPlainText -Force | ConvertFrom-SecureString | Set-Content -LiteralPath $protected
$cfg.Afip.Ambiente = 'Produccion'
$cfg.Afip.WsaaUrl = 'https://wsaa.afip.gov.ar/ws/services/LoginCms'
$cfg.Afip.WsfeUrl = 'https://servicios1.afip.gov.ar/wsfev1/service.asmx'
$cfg.Afip.RutaTicket = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../afip-tickets-produccion'))
$cfg.Afip.Certificado.Modo = 'Pfx'
$cfg.Afip.Certificado.RutaPfx = $pfx
$cfg.Afip.Certificado.PasswordPfx = $null
$cfg.Afip.Certificado.RutaPasswordProtegida = $protected
$cfg | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $configPath -Encoding utf8
$rsa.Dispose(); $pub.Dispose(); $check.Dispose(); $cert.Dispose()
Write-Output 'Producción configurada. Certificado verificado y protegido. No se emitió ningún comprobante.'
