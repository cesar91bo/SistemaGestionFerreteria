using System.Security.Cryptography.X509Certificates;
using SistemaGestionFerreteria.Application.Features.FacturacionElectronica.Models;

namespace SistemaGestionFerreteria.Infrastructure.Services.FacturacionElectronica;

public static class AfipCertificado
{
    public static X509Certificate2 Cargar(AfipOptions options)
    {
        X509Certificate2 cert;
        if (string.Equals(options.Certificado.Modo, "Pfx", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(options.Certificado.RutaPfx) || !File.Exists(options.Certificado.RutaPfx))
                throw new InvalidOperationException("No se encontró el archivo PFX configurado.");
            try { cert = new X509Certificate2(options.Certificado.RutaPfx, Password(options.Certificado), X509KeyStorageFlags.EphemeralKeySet); }
            catch { throw new InvalidOperationException("No se pudo abrir el PFX. Verifique el archivo y su contraseña."); }
        }
        else if (string.Equals(options.Certificado.Modo, "Store", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(options.Certificado.Serial)) throw new InvalidOperationException("Falta el serial del certificado.");
            cert = BuscarStore(options.Certificado.Serial) ?? throw new InvalidOperationException("No se encontró el certificado instalado.");
        }
        else throw new InvalidOperationException("El modo de certificado debe ser Pfx o Store.");
        try
        {
            if (!cert.HasPrivateKey) throw new InvalidOperationException("El certificado no tiene clave privada.");
            if (cert.NotBefore.ToUniversalTime() > DateTime.UtcNow || cert.NotAfter.ToUniversalTime() <= DateTime.UtcNow)
                throw new InvalidOperationException("El certificado está vencido o todavía no está vigente.");
            using var rsa = cert.GetRSAPrivateKey();
            if (rsa is null) throw new InvalidOperationException("No se pudo acceder a la clave privada RSA.");
            rsa.SignData(new byte[] { 1, 2, 3 }, System.Security.Cryptography.HashAlgorithmName.SHA256, System.Security.Cryptography.RSASignaturePadding.Pkcs1);
            return cert;
        }
        catch { cert.Dispose(); throw; }
    }

    private static string? Password(AfipCertificadoOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.RutaPasswordProtegida)) return options.PasswordPfx;
        if (!OperatingSystem.IsWindows()) throw new InvalidOperationException("La contraseña protegida requiere Windows.");
        var encrypted = Convert.FromHexString(File.ReadAllText(options.RutaPasswordProtegida).Trim());
        return System.Text.Encoding.Unicode.GetString(System.Security.Cryptography.ProtectedData.Unprotect(
            encrypted, null, System.Security.Cryptography.DataProtectionScope.CurrentUser));
    }

    private static X509Certificate2? BuscarStore(string serial)
    {
        foreach (var location in new[] { StoreLocation.CurrentUser, StoreLocation.LocalMachine })
        {
            using var store = new X509Store(StoreName.My, location);
            store.Open(OpenFlags.ReadOnly);
            var matches = store.Certificates.Find(X509FindType.FindBySerialNumber, serial.Replace(" ", ""), false);
            if (matches.Count > 0) return matches[0];
        }
        return null;
    }

    public static bool CuitValido(string? text)
    {
        var cuit = text?.Replace("-", "").Trim();
        if (cuit is null || cuit.Length != 11 || !cuit.All(char.IsAsciiDigit)) return false;
        int[] weights = { 5, 4, 3, 2, 7, 6, 5, 4, 3, 2 };
        var sum = Enumerable.Range(0, 10).Sum(i => (cuit[i] - '0') * weights[i]);
        var digit = 11 - sum % 11;
        digit = digit == 11 ? 0 : digit == 10 ? 9 : digit;
        return digit == cuit[10] - '0';
    }
}
