using Microsoft.Extensions.Options;
using SistemaGestionFerreteria.Application.Features.FacturacionElectronica.Models;
using SistemaGestionFerreteria.Application.Interfaces;

namespace SistemaGestionFerreteria.Infrastructure.Services.FacturacionElectronica;

public class AfipConfiguracionService : IAfipConfiguracionService
{
    private readonly AfipOptions _options;
    public AfipConfiguracionService(IOptions<AfipOptions> options) => _options = options.Value;

    public AfipEstadoConfiguracionViewModel ValidarConfiguracion()
    {
        var o = _options;
        var result = new AfipEstadoConfiguracionViewModel
        {
            Ambiente = o.Ambiente, Cuit = o.Cuit, PuntoVenta = o.PuntoVenta, Servicio = o.Servicio,
            WsaaUrl = o.WsaaUrl, WsfeUrl = o.WsfeUrl, ModoCertificado = o.Certificado.Modo, RutaPfx = o.Certificado.RutaPfx
        };
        if (!AfipCertificado.CuitValido(o.Cuit)) result.Errores.Add("El CUIT configurado es inválido.");
        if (o.PuntoVenta is < 1 or > 99998) result.Errores.Add("El punto de venta debe estar entre 1 y 99998.");
        if (o.Servicio != "wsfe") result.Errores.Add("El servicio WSAA debe ser wsfe.");
        if (o.LimiteConsumidorFinalSinDocumento <= 0) result.Errores.Add("El límite de identificación de consumidor final debe ser mayor a cero.");
        var homo = string.Equals(o.Ambiente, "Homologacion", StringComparison.OrdinalIgnoreCase);
        var prod = string.Equals(o.Ambiente, "Produccion", StringComparison.OrdinalIgnoreCase);
        if (!homo && !prod) result.Errores.Add("El ambiente debe ser Homologacion o Produccion.");
        var wsaa = homo ? "https://wsaahomo.afip.gov.ar/ws/services/LoginCms" : "https://wsaa.afip.gov.ar/ws/services/LoginCms";
        var wsfe = homo ? "https://wswhomo.afip.gov.ar/wsfev1/service.asmx" : "https://servicios1.afip.gov.ar/wsfev1/service.asmx";
        if (!string.Equals(o.WsaaUrl, wsaa, StringComparison.OrdinalIgnoreCase)) result.Errores.Add("La URL WSAA no coincide con el ambiente. Use el endpoint sin ?wsdl.");
        if (!string.Equals(o.WsfeUrl, wsfe, StringComparison.OrdinalIgnoreCase)) result.Errores.Add("La URL WSFE no coincide con el ambiente. Use el endpoint sin ?wsdl.");
        try
        {
            using var cert = AfipCertificado.Cargar(o);
            result.ExisteCertificado = true;
            if (prod && cert.Issuer.Contains("Computadores Test", StringComparison.OrdinalIgnoreCase))
                result.Errores.Add("El certificado es de homologación: no puede usarse para facturas reales.");
            if (homo && !cert.Issuer.Contains("Computadores Test", StringComparison.OrdinalIgnoreCase))
                result.Errores.Add("El certificado no es de homologación. Seleccione el certificado del ambiente de pruebas.");
        }
        catch (Exception ex) { result.Errores.Add(ex.Message); }
        result.ConfiguracionValida = result.Errores.Count == 0;
        return result;
    }
}
