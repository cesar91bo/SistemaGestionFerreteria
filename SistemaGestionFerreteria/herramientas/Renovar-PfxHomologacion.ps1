$ErrorActionPreference = 'Stop'
$configPath = Join-Path $PSScriptRoot '../SistemaGestionFerreteria/appsettings.json'
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
$publicCert = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new('C:\Certificados\MIL-DAN\homologacion.crt')
$cert = Get-Item -LiteralPath ('Cert:\LocalMachine\My\' + $publicCert.Thumbprint)
if (-not $cert.HasPrivateKey) { throw 'El certificado instalado no tiene clave privada.' }
if ($cert.NotAfter -le (Get-Date)) { throw 'El certificado está vencido.' }
$random = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Fill($random)
$password = [Convert]::ToBase64String($random)
$newPath = 'C:\Certificados\MIL-DAN\homologacion-renovada.pfx'
if (Test-Path -LiteralPath $newPath) { throw 'El PFX nuevo ya existe. No se sobrescribió.' }
$pfxBytes = $cert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Pfx, $password)
$check = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($pfxBytes, $password, [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::EphemeralKeySet)
if (-not $check.HasPrivateKey -or $check.Thumbprint -ne $cert.Thumbprint) { throw 'La verificación del nuevo PFX falló.' }
$rsa = [System.Security.Cryptography.X509Certificates.RSACertificateExtensions]::GetRSAPrivateKey($check)
$payload = [Text.Encoding]::UTF8.GetBytes('Verificacion local MIL-DAN')
$signature = $rsa.SignData($payload, [System.Security.Cryptography.HashAlgorithmName]::SHA256, [System.Security.Cryptography.RSASignaturePadding]::Pkcs1)
$pub = [System.Security.Cryptography.X509Certificates.RSACertificateExtensions]::GetRSAPublicKey($check)
if (-not $pub.VerifyData($payload, $signature, [System.Security.Cryptography.HashAlgorithmName]::SHA256, [System.Security.Cryptography.RSASignaturePadding]::Pkcs1)) { throw 'No se pudo verificar la firma.' }
[IO.File]::WriteAllBytes($newPath, $pfxBytes)
$config.Afip.Certificado.RutaPfx = $newPath
$protectedPath = Join-Path $PSScriptRoot 'certificado-homologacion/password-protegida-renovacion.txt'
ConvertTo-SecureString $password -AsPlainText -Force | ConvertFrom-SecureString | Set-Content -LiteralPath $protectedPath
$config.Afip.Certificado.PasswordPfx = $null
$config.Afip.Certificado | Add-Member -NotePropertyName RutaPasswordProtegida -NotePropertyValue ([IO.Path]::GetFullPath($protectedPath)) -Force
$config.Afip.WsaaUrl = 'https://wsaahomo.afip.gov.ar/ws/services/LoginCms'
$config.Afip.WsfeUrl = 'https://wswhomo.afip.gov.ar/wsfev1/service.asmx'
$config | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $configPath -Encoding utf8
$pub.Dispose()
$rsa.Dispose()
$check.Dispose()
$publicCert.Dispose()
Write-Output 'PFX nuevo creado, apertura y firma verificadas. Configuración de homologación actualizada. Archivo anterior conservado.'
