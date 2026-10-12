using Microsoft.Extensions.Options;
using SistemaGestionFerreteria.Application.Features.FacturacionElectronica.Models;
using SistemaGestionFerreteria.Application.Interfaces;
using System.Text;
using System.Xml.Linq;

namespace SistemaGestionFerreteria.Infrastructure.Services.FacturacionElectronica
{
    public class AfipWsfeService : IAfipWsfeService
    {
        private readonly AfipOptions _options;
        private readonly IAfipWsaaService _afipWsaaService;

        public AfipWsfeService(
            IOptions<AfipOptions> options,
            IAfipWsaaService afipWsaaService)
        {
            _options = options.Value;
            _afipWsaaService = afipWsaaService;
        }

        public async Task<AfipUltimoComprobanteResultado> ObtenerUltimoComprobanteAutorizadoAsync(
            int puntoVenta,
            int codigoTipoComprobanteAfip)
        {
            try
            {
                var login = await _afipWsaaService.ObtenerLoginTicketAsync();

                if (!login.Exitoso)
                {
                    return new AfipUltimoComprobanteResultado
                    {
                        Exitoso = false,
                        PuntoVenta = puntoVenta,
                        CodigoTipoComprobanteAfip = codigoTipoComprobanteAfip,
                        Error = "No se pudo obtener Token/Sign de WSAA. " + login.Error
                    };
                }

                var respuestaSoap = await EnviarConsultaUltimoComprobanteAsync(
                    login.Token!,
                    login.Sign!,
                    puntoVenta,
                    codigoTipoComprobanteAfip);

                return LeerRespuestaUltimoComprobante(
                    respuestaSoap,
                    puntoVenta,
                    codigoTipoComprobanteAfip);
            }
            catch (Exception ex)
            {
                return new AfipUltimoComprobanteResultado
                {
                    Exitoso = false,
                    PuntoVenta = puntoVenta,
                    CodigoTipoComprobanteAfip = codigoTipoComprobanteAfip,
                    Error = ex.Message
                };
            }
        }

        private async Task<string> EnviarConsultaUltimoComprobanteAsync(
            string token,
            string sign,
            int puntoVenta,
            int codigoTipoComprobanteAfip)
        {
            if (string.IsNullOrWhiteSpace(_options.WsfeUrl))
                throw new Exception("No está configurada la URL de WSFE.");

            var cuit = _options.Cuit?.Replace("-", "").Trim();

            if (string.IsNullOrWhiteSpace(cuit))
                throw new Exception("No está configurado el CUIT de AFIP.");

            var soapEnvelope =
$@"<?xml version=""1.0"" encoding=""UTF-8""?>
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/""
                  xmlns:ar=""http://ar.gov.afip.dif.FEV1/"">
   <soapenv:Header/>
   <soapenv:Body>
      <ar:FECompUltimoAutorizado>
         <ar:Auth>
            <ar:Token>{token}</ar:Token>
            <ar:Sign>{sign}</ar:Sign>
            <ar:Cuit>{cuit}</ar:Cuit>
         </ar:Auth>
         <ar:PtoVta>{puntoVenta}</ar:PtoVta>
         <ar:CbteTipo>{codigoTipoComprobanteAfip}</ar:CbteTipo>
      </ar:FECompUltimoAutorizado>
   </soapenv:Body>
</soapenv:Envelope>";

            using var client = new HttpClient();

            using var request = new HttpRequestMessage(HttpMethod.Post, _options.WsfeUrl);
            request.Content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            request.Headers.Add("SOAPAction", "http://ar.gov.afip.dif.FEV1/FECompUltimoAutorizado");

            using var response = await client.SendAsync(request);

            var contenido = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error HTTP WSFE: {(int)response.StatusCode}. Respuesta: {contenido}");
            }

            return contenido;
        }

        private AfipUltimoComprobanteResultado LeerRespuestaUltimoComprobante(
            string respuestaSoap,
            int puntoVenta,
            int codigoTipoComprobanteAfip)
        {
            var xml = XDocument.Parse(respuestaSoap);

            var errores = xml
                .Descendants()
                .Where(x => x.Name.LocalName == "Err")
                .Select(x =>
                {
                    var codigo = x.Descendants().FirstOrDefault(e => e.Name.LocalName == "Code")?.Value;
                    var mensaje = x.Descendants().FirstOrDefault(e => e.Name.LocalName == "Msg")?.Value;
                    return $"{codigo} - {mensaje}";
                })
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            if (errores.Any())
            {
                return new AfipUltimoComprobanteResultado
                {
                    Exitoso = false,
                    PuntoVenta = puntoVenta,
                    CodigoTipoComprobanteAfip = codigoTipoComprobanteAfip,
                    Error = string.Join(" | ", errores)
                };
            }

            var cbteNroText = xml
                .Descendants()
                .FirstOrDefault(x => x.Name.LocalName == "CbteNro")
                ?.Value;

            if (!int.TryParse(cbteNroText, out var ultimoNumero))
            {
                return new AfipUltimoComprobanteResultado
                {
                    Exitoso = false,
                    PuntoVenta = puntoVenta,
                    CodigoTipoComprobanteAfip = codigoTipoComprobanteAfip,
                    Error = "WSFE respondió, pero no se pudo leer CbteNro. Respuesta: " + respuestaSoap
                };
            }

            return new AfipUltimoComprobanteResultado
            {
                Exitoso = true,
                PuntoVenta = puntoVenta,
                CodigoTipoComprobanteAfip = codigoTipoComprobanteAfip,
                UltimoNumero = ultimoNumero
            };
        }
    }
}