using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Extensions.Options;
using SistemaGestionFerreteria.Application.Features.FacturacionElectronica.Models;
using SistemaGestionFerreteria.Application.Interfaces;

namespace SistemaGestionFerreteria.Infrastructure.Services.FacturacionElectronica;

public class AfipWsaaService : IAfipWsaaService
{
    private readonly AfipOptions _options;
    private readonly HttpClient _http;
    private readonly IAfipConfiguracionService _configuration;
    private static readonly ConcurrentDictionary<string, AfipLoginResultado> Cache = new();
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();

    public AfipWsaaService(IOptions<AfipOptions> options, HttpClient http, IAfipConfiguracionService configuration)
    { _options = options.Value; _http = http; _configuration = configuration; }

    public async Task<AfipLoginResultado> ObtenerLoginTicketAsync()
    {
        try
        {
            var state = _configuration.ValidarConfiguracion();
            if (!state.ConfiguracionValida) throw new InvalidOperationException(string.Join(" ", state.Errores));
            using var cert = AfipCertificado.Cargar(_options);
            var identity = _options.WsaaUrl + "|" + _options.Servicio + "|" + _options.Cuit + "|" + cert.Thumbprint;
            var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
            var gate = Locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync();
            try
            {
                if (Cache.TryGetValue(key, out var cached) && Valido(cached)) return cached;
                // DPAPI: el ticket persistido solo puede abrirlo la cuenta Windows que lo obtuvo.
                var path = Path.Combine(_options.RutaTicket ?? Path.Combine(AppContext.BaseDirectory, "afip-tickets"), key + ".bin");
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                using var fileLock = await BloquearArchivoAsync(path + ".lock");
                if (OperatingSystem.IsWindows() && File.Exists(path))
                {
                    try
                    {
                        var bytes = ProtectedData.Unprotect(await File.ReadAllBytesAsync(path), null, DataProtectionScope.CurrentUser);
                        var saved = JsonSerializer.Deserialize<AfipLoginResultado>(bytes);
                        if (saved is not null && Valido(saved)) { Cache[key] = saved; return saved; }
                    }
                    catch (CryptographicException) { /* Otra cuenta o ticket dañado: renovar. */ }
                    catch (JsonException) { /* Ticket inválido: renovar. */ }
                }
                var now = DateTimeOffset.UtcNow;
                var tra = new XElement("loginTicketRequest", new XAttribute("version", "1.0"),
                    new XElement("header", new XElement("uniqueId", now.ToUnixTimeSeconds()),
                        new XElement("generationTime", now.AddMinutes(-10).ToString("o")),
                        new XElement("expirationTime", now.AddHours(12).ToString("o"))),
                    new XElement("service", _options.Servicio));
                var cms = new SignedCms(new ContentInfo(Encoding.UTF8.GetBytes(tra.ToString(SaveOptions.DisableFormatting))), false);
                cms.ComputeSignature(new CmsSigner(cert) { IncludeOption = System.Security.Cryptography.X509Certificates.X509IncludeOption.EndCertOnly });
                XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";
                XNamespace wsaa = "http://wsaa.view.sua.dvadac.desein.afip.gov";
                var envelope = new XElement(soap + "Envelope", new XElement(soap + "Body", new XElement(wsaa + "loginCms", new XElement(wsaa + "in0", Convert.ToBase64String(cms.Encode())))));
                using var request = new HttpRequestMessage(HttpMethod.Post, _options.WsaaUrl);
                request.Content = new StringContent(envelope.ToString(SaveOptions.DisableFormatting), Encoding.UTF8, "text/xml");
                request.Headers.Add("SOAPAction", "\"\"");
                using var response = await _http.SendAsync(request);
                var text = await response.Content.ReadAsStringAsync();
                XElement xml;
                try { xml = XElement.Parse(text); } catch { throw new InvalidOperationException("Respuesta WSAA inválida (HTTP " + (int)response.StatusCode + ")."); }
                var fault = Valor(xml, "faultstring");
                if (fault is not null) throw new InvalidOperationException("WSAA: " + fault + (fault.Contains("alreadyAuthenticated", StringComparison.OrdinalIgnoreCase) ? ". Hay un ticket vigente obtenido por otro proceso. Espere su vencimiento o use el ticket persistido de ese proceso." : ""));
                if (!response.IsSuccessStatusCode) throw new InvalidOperationException("HTTP WSAA: " + (int)response.StatusCode);
                var ticketXml = Valor(xml, "loginCmsReturn") ?? throw new InvalidOperationException("WSAA no devolvió loginCmsReturn.");
                var ticket = XElement.Parse(ticketXml);
                if (!DateTimeOffset.TryParse(Valor(ticket, "expirationTime"), CultureInfo.InvariantCulture, DateTimeStyles.None, out var expiry))
                    throw new InvalidOperationException("WSAA no devolvió un vencimiento válido.");
                var result = new AfipLoginResultado { Exitoso = true, Token = Valor(ticket, "token"), Sign = Valor(ticket, "sign"), ExpirationTime = expiry.UtcDateTime };
                if (!Valido(result)) throw new InvalidOperationException("WSAA no devolvió credenciales vigentes.");
                Cache[key] = result;
                if (OperatingSystem.IsWindows())
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    var protectedBytes = ProtectedData.Protect(JsonSerializer.SerializeToUtf8Bytes(result), null, DataProtectionScope.CurrentUser);
                    var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                    await File.WriteAllBytesAsync(temporary, protectedBytes);
                    File.Move(temporary, path, true);
                }
                return result;
            }
            finally { gate.Release(); }
        }
        catch (Exception ex) { return new AfipLoginResultado { Exitoso = false, Error = ex.Message }; }
    }
    private static bool Valido(AfipLoginResultado login) => login.Exitoso && !string.IsNullOrWhiteSpace(login.Token) && !string.IsNullOrWhiteSpace(login.Sign) && login.ExpirationTime?.ToUniversalTime() > DateTime.UtcNow.AddMinutes(5);
    private static async Task<FileStream> BloquearArchivoAsync(string path)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            try { return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
            catch (IOException) { await Task.Delay(200); }
        }
        throw new InvalidOperationException("Otro proceso está obteniendo el ticket WSAA. Vuelva a intentar.");
    }
    private static string? Valor(XElement xml, string name) => xml.Descendants().FirstOrDefault(x => x.Name.LocalName == name)?.Value;
}
