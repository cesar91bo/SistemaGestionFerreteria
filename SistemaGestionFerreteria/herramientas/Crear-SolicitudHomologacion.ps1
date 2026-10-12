$ErrorActionPreference = 'Stop'
$destination = Join-Path $PSScriptRoot 'certificado-homologacion'
if (Test-Path -LiteralPath $destination) { throw 'Ya existe una solicitud. No se sobrescribieron archivos.' }
$rsa = [System.Security.Cryptography.RSA]::Create(2048)
$subject = [System.Security.Cryptography.X509Certificates.X500DistinguishedName]::new('CN=mildanhomo2026, SERIALNUMBER=CUIT 20252443011, O=MIL-DAN, C=AR')
$request = [System.Security.Cryptography.X509Certificates.CertificateRequest]::new($subject, $rsa, [System.Security.Cryptography.HashAlgorithmName]::SHA256, [System.Security.Cryptography.RSASignaturePadding]::Pkcs1)
$random = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Fill($random)
$password = [Convert]::ToBase64String($random)
$pbe = [System.Security.Cryptography.PbeParameters]::new([System.Security.Cryptography.PbeEncryptionAlgorithm]::Aes256Cbc, [System.Security.Cryptography.HashAlgorithmName]::SHA256, 100000)
$encryptedKey = $rsa.ExportEncryptedPkcs8PrivateKey($password, $pbe)
New-Item -ItemType Directory -Path $destination | Out-Null
[IO.File]::WriteAllText((Join-Path $destination 'mildanhomo2026.csr'), $request.CreateSigningRequestPem())
[IO.File]::WriteAllBytes((Join-Path $destination 'clave-privada-cifrada.bin'), $encryptedKey)
ConvertTo-SecureString $password -AsPlainText -Force | ConvertFrom-SecureString | Set-Content -LiteralPath (Join-Path $destination 'password-protegida.txt')
$rsa.Dispose()
Write-Output 'Solicitud CSR creada. Clave cifrada y contraseña protegida para este usuario de Windows. Solo el archivo .csr se entrega a WSASS.'
