$ErrorActionPreference = 'Stop'
$folder = Join-Path $PSScriptRoot 'certificado-homologacion'
$secure = (Get-Content -LiteralPath (Join-Path $folder 'password-protegida.txt') -Raw).Trim() | ConvertTo-SecureString
$password = [System.Net.NetworkCredential]::new('', $secure).Password
Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
public static class MilDanPfx {
    public static string Create(string folder, string password) {
        using var rsa = RSA.Create();
        rsa.ImportEncryptedPkcs8PrivateKey(password, File.ReadAllBytes(Path.Combine(folder, "clave-privada-cifrada.bin")), out int read);
        using var cert = X509Certificate2.CreateFromPem(File.ReadAllText(Path.Combine(folder, "homologacion-nueva.crt")));
        if (DateTime.Now < cert.NotBefore || DateTime.Now > cert.NotAfter) throw new Exception("Certificado fuera de vigencia.");
        using var complete = cert.CopyWithPrivateKey(rsa);
        byte[] bytes = complete.Export(X509ContentType.Pfx, password);
        using var check = X509CertificateLoader.LoadPkcs12(bytes, password, X509KeyStorageFlags.EphemeralKeySet);
        using var key = check.GetRSAPrivateKey();
        using var pub = check.GetRSAPublicKey();
        byte[] data = System.Text.Encoding.UTF8.GetBytes("Verificacion MIL-DAN");
        byte[] signature = key.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        if (!pub.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)) throw new Exception("Firma inválida.");
        var path = Path.Combine(folder, "homologacion-nueva.pfx");
        if (File.Exists(path)) throw new Exception("El PFX ya existe; no se sobrescribió.");
        File.WriteAllBytes(path, bytes);
        return path;
    }
}
'@
$pfxPath = [MilDanPfx]::Create($folder, $password)
$configPath = Join-Path $PSScriptRoot '../SistemaGestionFerreteria/appsettings.json'
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
$config.Afip.Certificado.RutaPfx = [IO.Path]::GetFullPath($pfxPath)
$config.Afip.Certificado.PasswordPfx = $null
$config.Afip.Certificado | Add-Member -NotePropertyName RutaPasswordProtegida -NotePropertyValue ([IO.Path]::GetFullPath((Join-Path $folder 'password-protegida.txt'))) -Force
$config.Afip.WsaaUrl = 'https://wsaahomo.afip.gov.ar/ws/services/LoginCms'
$config.Afip.WsfeUrl = 'https://wswhomo.afip.gov.ar/wsfev1/service.asmx'
$config | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $configPath -Encoding utf8
Write-Output 'PFX nuevo creado y configurado. Certificado y clave coinciden. Apertura y firma verificadas.'
