using Microsoft.Extensions.Options;
using SistemaGestionFerreteria.Application.Features.FacturacionElectronica.Models;
using SistemaGestionFerreteria.Application.Interfaces;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Linq;

namespace SistemaGestionFerreteria.Infrastructure.Services.FacturacionElectronica
{
    public class AfipWsaaService : IAfipWsaaService
    {
        private readonly AfipOptions _options;

        private static AfipLoginResultado? _loginCache;
        private static readonly object _lock = new();

        public AfipWsaaService(IOptions<AfipOptions> options)
        {
            _options = options.Value;
        }

        public async Task<AfipLoginResultado> ObtenerLoginTicketAsync()
        {
            try
            {
                var loginCache = ObtenerLoginCacheValido();

                if (loginCache is not null)
                    return loginCache;

                var certificado = CargarCertificado();

                var traXml = CrearLoginTicketRequestXml();

                var cmsFirmadoBase64 = FirmarCms(traXml, certificado);

                var respuestaSoap = await EnviarLoginCmsAsync(cmsFirmadoBase64);

                var resultado = LeerRespuestaWsaa(respuestaSoap);

                if (resultado.Exitoso)
                {
                    GuardarLoginCache(resultado);
                }

                return resultado;
            }
            catch (Exception ex)
            {
                return new AfipLoginResultado
                {
                    Exitoso = false,
                    Error = ex.Message
                };
            }
        }

        private AfipLoginResultado? ObtenerLoginCacheValido()
        {
            lock (_lock)
            {
                if (_loginCache is null)
                    return null;

                if (!_loginCache.Exitoso)
                    return null;

                if (string.IsNullOrWhiteSpace(_loginCache.Token) || string.IsNullOrWhiteSpace(_loginCache.Sign))
                    return null;

                if (!_loginCache.ExpirationTime.HasValue)
                    return null;

                // Margen de seguridad: si vence en menos de 5 minutos, pedimos uno nuevo.
                if (_loginCache.ExpirationTime.Value <= DateTime.Now.AddMinutes(5))
                    return null;

                return _loginCache;
            }
        }

        private void GuardarLoginCache(AfipLoginResultado resultado)
        {
            lock (_lock)
            {
                _loginCache = resultado;
            }
        }

        private X509Certificate2 CargarCertificado()
        {
            if (!string.Equals(_options.Certificado.Modo, "Pfx", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Por ahora el login WSAA está preparado para usar certificado en modo Pfx.");

            if (string.IsNullOrWhiteSpace(_options.Certificado.RutaPfx))
                throw new Exception("No está configurada la ruta del archivo PFX.");

            if (!File.Exists(_options.Certificado.RutaPfx))
                throw new Exception("No se encontró el archivo PFX en la ruta configurada.");

            return new X509Certificate2(
                _options.Certificado.RutaPfx,
                _options.Certificado.PasswordPfx,
                X509KeyStorageFlags.MachineKeySet |
                X509KeyStorageFlags.PersistKeySet |
                X509KeyStorageFlags.Exportable
            );
        }

        private string CrearLoginTicketRequestXml()
        {
            var ahora = DateTime.Now;

            var generationTime = ahora.AddMinutes(-10).ToString("yyyy-MM-ddTHH:mm:sszzz");
            var expirationTime = ahora.AddHours(12).ToString("yyyy-MM-ddTHH:mm:sszzz");

            var uniqueId = DateTimeOffset.Now.ToUnixTimeSeconds();

            var xml =
$@"<?xml version=""1.0"" encoding=""UTF-8""?>
<loginTicketRequest version=""1.0"">
  <header>
    <uniqueId>{uniqueId}</uniqueId>
    <generationTime>{generationTime}</generationTime>
    <expirationTime>{expirationTime}</expirationTime>
  </header>
  <service>{_options.Servicio}</service>
</loginTicketRequest>";

            return xml;
        }

        private string FirmarCms(string xml, X509Certificate2 certificado)
        {
            var bytesXml = Encoding.UTF8.GetBytes(xml);

            var contentInfo = new ContentInfo(bytesXml);

            var signedCms = new SignedCms(contentInfo, detached: false);

            var cmsSigner = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, certificado)
            {
                IncludeOption = X509IncludeOption.EndCertOnly
            };

            signedCms.ComputeSignature(cmsSigner);

            var encoded = signedCms.Encode();

            return Convert.ToBase64String(encoded);
        }

        private async Task<string> EnviarLoginCmsAsync(string cmsFirmadoBase64)
        {
            if (string.IsNullOrWhiteSpace(_options.WsaaUrl))
                throw new Exception("No está configurada la URL de WSAA.");

            var soapEnvelope =
$@"<?xml version=""1.0"" encoding=""UTF-8""?>
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/""
                  xmlns:wsaa=""http://wsaa.view.sua.dvadac.desein.afip.gov"">
   <soapenv:Header/>
   <soapenv:Body>
      <wsaa:loginCms>
         <wsaa:in0>{cmsFirmadoBase64}</wsaa:in0>
      </wsaa:loginCms>
   </soapenv:Body>
</soapenv:Envelope>";

            using var client = new HttpClient();

            using var request = new HttpRequestMessage(HttpMethod.Post, _options.WsaaUrl);
            request.Content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            request.Headers.Add("SOAPAction", "");

            using var response = await client.SendAsync(request);

            var contenido = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error HTTP WSAA: {(int)response.StatusCode}. Respuesta: {contenido}");
            }

            return contenido;
        }

        private AfipLoginResultado LeerRespuestaWsaa(string respuestaSoap)
        {
            var soap = XDocument.Parse(respuestaSoap);

            var loginCmsReturn = soap
                .Descendants()
                .FirstOrDefault(x => x.Name.LocalName == "loginCmsReturn")
                ?.Value;

            if (string.IsNullOrWhiteSpace(loginCmsReturn))
            {
                return new AfipLoginResultado
                {
                    Exitoso = false,
                    Error = "WSAA no devolvió loginCmsReturn. Respuesta: " + respuestaSoap
                };
            }

            var xmlTicket = XDocument.Parse(loginCmsReturn);

            var token = xmlTicket
                .Descendants()
                .FirstOrDefault(x => x.Name.LocalName == "token")
                ?.Value;

            var sign = xmlTicket
                .Descendants()
                .FirstOrDefault(x => x.Name.LocalName == "sign")
                ?.Value;

            var expirationText = xmlTicket
                .Descendants()
                .FirstOrDefault(x => x.Name.LocalName == "expirationTime")
                ?.Value;

            DateTime? expirationTime = null;

            if (DateTime.TryParse(expirationText, out var fechaExpiracion))
            {
                expirationTime = fechaExpiracion;
            }

            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(sign))
            {
                return new AfipLoginResultado
                {
                    Exitoso = false,
                    Error = "WSAA respondió, pero no se pudo obtener Token o Sign."
                };
            }

            return new AfipLoginResultado
            {
                Exitoso = true,
                Token = token,
                Sign = sign,
                ExpirationTime = expirationTime
            };
        }
    }
}