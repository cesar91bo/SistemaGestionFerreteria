using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Options;
using SistemaGestionFerreteria.Application.Features.FacturacionElectronica.Models;
using SistemaGestionFerreteria.Application.Interfaces;

namespace SistemaGestionFerreteria.Infrastructure.Services.FacturacionElectronica;

public class AfipWsfeService : IAfipWsfeService
{
    private static readonly XNamespace Ns = "http://ar.gov.afip.dif.FEV1/";
    private readonly AfipOptions _options;
    private readonly IAfipWsaaService _wsaa;
    private readonly HttpClient _http;
    public AfipWsfeService(IOptions<AfipOptions> options, IAfipWsaaService wsaa, HttpClient http)
    { _options = options.Value; _wsaa = wsaa; _http = http; }

    public async Task<AfipUltimoComprobanteResultado> ObtenerUltimoComprobanteAutorizadoAsync(int puntoVenta, int codigoTipoComprobanteAfip)
    {
        var result = new AfipUltimoComprobanteResultado { PuntoVenta = puntoVenta, CodigoTipoComprobanteAfip = codigoTipoComprobanteAfip };
        try
        {
            if (puntoVenta <= 0 || codigoTipoComprobanteAfip <= 0) throw new InvalidOperationException("Punto de venta o tipo inválido.");
            var xml = await EnviarAsync("FECompUltimoAutorizado", new XElement(Ns + "PtoVta", puntoVenta), new XElement(Ns + "CbteTipo", codigoTipoComprobanteAfip));
            var error = Mensajes(xml, "Err");
            if (error is not null) throw new InvalidOperationException(error);
            if (!int.TryParse(Valor(xml, "CbteNro"), out var number)) throw new InvalidOperationException("WSFE no devolvió CbteNro.");
            result.UltimoNumero = number;
            result.Exitoso = true;
        }
        catch (Exception ex) { result.Error = ex.Message; }
        return result;
    }

    public async Task<string?> ValidarPuntoVentaAsync()
    {
        try
        {
            var xml = await EnviarAsync("FEParamGetPtosVenta");
            var error = Mensajes(xml, "Err");
            if (error is not null) return error;
            var point = xml.Descendants().FirstOrDefault(x => x.Name.LocalName == "PtoVenta" && Valor(x, "Nro") == _options.PuntoVenta.ToString(CultureInfo.InvariantCulture));
            if (point is null) return "El punto de venta " + _options.PuntoVenta + " no está habilitado en WSFE para este CUIT.";
            if (Valor(point, "Bloqueado") != "N") return "El punto de venta figura bloqueado en ARCA.";
            var baja = Valor(point, "FchBaja");
            if (!string.IsNullOrWhiteSpace(baja) && baja != "NULL") return "El punto de venta tiene una fecha de baja en ARCA.";
            var emision = Valor(point, "EmisionTipo")?.Trim();
            if (!string.Equals(emision, "CAE", StringComparison.OrdinalIgnoreCase) &&
                !(emision?.StartsWith("CAE -", StringComparison.OrdinalIgnoreCase) ?? false))
                return "El punto de venta informa el tipo de emisión '" + emision + "', en lugar de CAE.";
            return null;
        }
        catch (Exception ex) { return ex.Message; }
    }

    public async Task<AfipAutorizacionResultado> AutorizarAsync(string solicitudXml)
    {
        try
        {
            var req = XElement.Parse(solicitudXml);
            var xml = await EnviarAsync("FECAESolicitar", req);
            var errors = Mensajes(xml, "Err");
            var detail = xml.Descendants().FirstOrDefault(x => x.Name.LocalName == "FECAEDetResponse");
            if (detail is null) return new() { Rechazado = errors is not null, Error = errors ?? "Respuesta sin detalle de autorización. Consultar antes de reenviar." };
            var submitted = req.Descendants(Ns + "FECAEDetRequest").Single();
            foreach (var field in new[] { "Concepto", "DocTipo", "DocNro", "CbteDesde", "CbteHasta", "CbteFch" })
                if (Valor(detail, field) != Valor(submitted, field))
                    return new() { Error = "La respuesta WSFE no corresponde al comprobante solicitado." };
            var result = Valor(detail, "Resultado");
            var observations = Mensajes(detail, "Obs");
            if (result == "R") return new() { Rechazado = true, Error = errors ?? observations ?? "Rechazado por ARCA." };
            if (result != "A") return new() { Error = errors ?? "Autorización indeterminada. Consultar antes de reenviar." };
            return LeerAutorizacion(detail, observations);
        }
        catch (Exception ex) { return new() { Error = "No se pudo confirmar la autorización: " + ex.Message }; }
    }

    public async Task<AfipAutorizacionResultado> ConsultarAsync(string solicitudXml)
    {
        try
        {
            var req = XElement.Parse(solicitudXml);
            var cab = req.Element(Ns + "FeCabReq")!;
            var det = req.Descendants(Ns + "FECAEDetRequest").Single();
            var xml = await EnviarAsync("FECompConsultar", new XElement(Ns + "FeCompConsReq",
                new XElement(Ns + "CbteTipo", Valor(cab, "CbteTipo")), new XElement(Ns + "CbteNro", Valor(det, "CbteDesde")),
                new XElement(Ns + "PtoVta", Valor(cab, "PtoVta"))));
            var errs = xml.Descendants().Where(x => x.Name.LocalName == "Err").ToList();
            // Solo 602 prueba ausencia; un error de conexión no la prueba.
            if (errs.Count > 0) return new() { NoEncontrado = errs.All(x => Valor(x, "Code") == "602"), Error = Mensajes(xml, "Err") };
            var detail = xml.Descendants().FirstOrDefault(x => x.Name.LocalName == "ResultGet");
            if (detail is null) return new() { Error = "WSFE no devolvió ResultGet." };
            foreach (var field in new[] { "PtoVta", "CbteTipo" })
                if (Valor(detail, field) != Valor(cab, field))
                    return new() { Error = "La serie registrada en ARCA no coincide (" + field + ")." };
            foreach (var field in new[] { "Concepto", "DocTipo", "DocNro", "CbteDesde", "CbteHasta", "CbteFch", "MonId", "CondicionIVAReceptorId" })
                if (Valor(detail, field) != Valor(det, field))
                    return new() { Error = "El comprobante de ARCA no coincide con este intento (" + field + ")." };
            foreach (var field in new[] { "ImpTotal", "ImpTotConc", "ImpNeto", "ImpOpEx", "ImpIVA", "ImpTrib", "MonCotiz" })
                if (!decimal.TryParse(Valor(detail, field), NumberStyles.Number, CultureInfo.InvariantCulture, out var actual) ||
                    !decimal.TryParse(Valor(det, field), NumberStyles.Number, CultureInfo.InvariantCulture, out var expected) || actual != expected)
                    return new() { Error = "Los importes registrados en ARCA no coinciden (" + field + ")." };
            foreach (var field in new[] { "Iva", "CbtesAsoc" })
            {
                var expected = det.Elements().FirstOrDefault(x => x.Name.LocalName == field);
                var actual = detail.Elements().FirstOrDefault(x => x.Name.LocalName == field);
                if (FirmaLista(expected) != FirmaLista(actual))
                    return new() { Error = "El detalle registrado en ARCA no coincide (" + field + ")." };
            }
            if (Valor(detail, "Resultado") != "A" || Valor(detail, "EmisionTipo") != "CAE")
                return new() { Error = "El comprobante consultado no está autorizado por CAE." };
            return LeerAutorizacion(new XElement("autorizacion", new XElement("CAE", Valor(detail, "CodAutorizacion")),
                new XElement("CAEFchVto", Valor(detail, "FchVto"))), Mensajes(detail, "Obs"));
        }
        catch (Exception ex) { return new() { Error = "No se pudo consultar el comprobante: " + ex.Message }; }
    }

    private static AfipAutorizacionResultado LeerAutorizacion(XElement detail, string? observations)
    {
        var cae = Valor(detail, "CAE");
        if (cae is null || cae.Length != 14 || !cae.All(char.IsAsciiDigit) ||
            !DateTime.TryParseExact(Valor(detail, "CAEFchVto"), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var expiry))
            return new() { Error = "Aprobación sin CAE/vencimiento válido. Consultar el comprobante." };
        return new() { Autorizado = true, Cae = cae, Vencimiento = expiry, Observaciones = observations };
    }

    private async Task<XElement> EnviarAsync(string operation, params XElement[] parameters)
    {
        var login = await _wsaa.ObtenerLoginTicketAsync();
        if (!login.Exitoso) throw new InvalidOperationException("Login WSAA: " + login.Error);
        XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";
        var envelope = new XElement(soap + "Envelope",
            new XAttribute(XNamespace.Xmlns + "soapenv", soap), new XAttribute(XNamespace.Xmlns + "ar", Ns),
            new XElement(soap + "Body", new XElement(Ns + operation,
            new XElement(Ns + "Auth", new XElement(Ns + "Token", login.Token), new XElement(Ns + "Sign", login.Sign),
                new XElement(Ns + "Cuit", _options.Cuit.Replace("-", "").Trim())), parameters)));
        using var request = new HttpRequestMessage(HttpMethod.Post, _options.WsfeUrl);
        request.Content = new StringContent("<?xml version=\"1.0\" encoding=\"utf-8\"?>" + envelope.ToString(SaveOptions.DisableFormatting), Encoding.UTF8, "text/xml");
        request.Headers.Add("SOAPAction", "\"" + Ns.NamespaceName + operation + "\"");
        using var response = await _http.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        XElement xml;
        try
        {
            xml = XElement.Parse(text);
            if (xml.Name != soap + "Envelope") throw new FormatException("La respuesta no es SOAP.");
        }
        catch
        {
            if ((int)response.StatusCode >= 500)
                throw new InvalidOperationException("El servicio WSFE de ARCA (" + _options.Ambiente + ") devolvió un error interno HTTP " + (int)response.StatusCode + ". No se pudo confirmar la operación. Vuelva a consultar más tarde; si había un envío pendiente, use Consultar / reintentar ARCA sobre ese mismo comprobante.");
            throw new InvalidOperationException("Respuesta WSFE inválida (HTTP " + (int)response.StatusCode + ").");
        }
        var fault = Valor(xml, "faultstring");
        if (fault is not null) throw new InvalidOperationException("WSFE: " + fault);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException("HTTP WSFE: " + (int)response.StatusCode);
        return xml;
    }

    private static string? Valor(XElement xml, string name) => xml.Descendants().FirstOrDefault(x => x.Name.LocalName == name)?.Value;
    private static string FirmaLista(XElement? xml) => xml is null ? "" : string.Join(";",
        xml.Elements().Select(item => string.Join("|", item.Elements().OrderBy(x => x.Name.LocalName).Select(x =>
            x.Name.LocalName + "=" + (decimal.TryParse(x.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
                ? number.ToString("G29", CultureInfo.InvariantCulture) : x.Value)))).OrderBy(x => x));
    private static string? Mensajes(XElement xml, string name)
    {
        var messages = xml.Descendants().Where(x => x.Name.LocalName == name).Select(x => Valor(x, "Code") + " - " + Valor(x, "Msg")).ToList();
        return messages.Count == 0 ? null : string.Join(" | ", messages);
    }
}
