using System.Globalization;
using System.Xml.Linq;
using SistemaGestionFerreteria.Application.Features.Facturacion.Models;
using SistemaGestionFerreteria.Application.Features.FacturacionElectronica.Models;
using SistemaGestionFerreteria.Domain.Entities;
using SistemaGestionFerreteria.Domain.Enums;

namespace SistemaGestionFerreteria.Infrastructure.Services.FacturacionElectronica;

public static class AfipSolicitudFactory
{
    private static readonly XNamespace Ns = "http://ar.gov.afip.dif.FEV1/";
    public static string Crear(Factura factura, Cliente cliente, Factura? asociada, AfipOptions options)
    {
        var claseA = factura.CodigoComprobanteAfip is 1 or 3;
        var condition = cliente.RegimenImpositivo switch
        {
            TipoRegimenImpositivo.ResponsableInscripto => 1,
            TipoRegimenImpositivo.Exento => 4,
            TipoRegimenImpositivo.ConsumidorFinal => 5,
            TipoRegimenImpositivo.Monotributista => 6,
            _ => throw new InvalidOperationException("Debe especificar una condición IVA válida del cliente.")
        };
        if (claseA && condition is not (1 or 6)) throw new InvalidOperationException("Factura A requiere receptor responsable inscripto o monotributista.");
        if (!claseA && condition is 1 or 6) throw new InvalidOperationException("Para este receptor corresponde un comprobante A.");
        var cuit = cliente.Cuit?.Replace("-", "").Trim();
        long document = 0;
        var docType = 99;
        if (!string.IsNullOrWhiteSpace(cuit))
        {
            if (!AfipCertificado.CuitValido(cuit)) throw new InvalidOperationException("El CUIT del cliente es inválido.");
            document = long.Parse(cuit, CultureInfo.InvariantCulture);
            docType = 80;
        }
        else if (condition != 5) throw new InvalidOperationException("Debe cargar el CUIT del cliente para esta condición IVA.");
        else if (!string.IsNullOrWhiteSpace(cliente.Documento))
        {
            var dni = cliente.Documento.Replace(".", "").Replace(" ", "").Trim();
            if (!long.TryParse(dni, out document) || document <= 0 || dni.Length is < 6 or > 8)
                throw new InvalidOperationException("El DNI del cliente debe contener entre 6 y 8 dígitos.");
            docType = 96;
        }
        if (condition == 5 && factura.Total >= options.LimiteConsumidorFinalSinDocumento && docType == 99)
            throw new InvalidOperationException("Por el importe de la operación debe identificar al consumidor final con DNI o CUIT.");
        var today = FechaArgentina();
        if (factura.Fecha.Date < today.AddDays(-5) || factura.Fecha.Date > today.AddDays(5))
            throw new InvalidOperationException("Para venta de productos la fecha debe estar dentro de los 5 días de la fecha actual.");
        var detail = new XElement(Ns + "FECAEDetRequest",
            E("Concepto", 1), E("DocTipo", docType), E("DocNro", document),
            E("CbteDesde", factura.NumeroComprobante), E("CbteHasta", factura.NumeroComprobante),
            E("CbteFch", factura.Fecha.ToString("yyyyMMdd")),
            E("ImpTotal", M(factura.Total)), E("ImpTotConc", "0.00"),
            E("ImpNeto", M(factura.Subtotal - factura.TotalExento)), E("ImpOpEx", M(factura.TotalExento)),
            E("ImpTrib", "0.00"), E("ImpIVA", M(factura.TotalIva21 + factura.TotalIva105 + factura.TotalIva27)),
            E("MonId", "PES"), E("MonCotiz", "1"), E("CondicionIVAReceptorId", condition));
        if (asociada is not null)
            detail.Add(new XElement(Ns + "CbtesAsoc", new XElement(Ns + "CbteAsoc",
                E("Tipo", asociada.CodigoComprobanteAfip), E("PtoVta", asociada.PuntoVenta), E("Nro", asociada.NumeroComprobante),
                E("Cuit", options.Cuit.Replace("-", "").Trim()), E("CbteFch", asociada.Fecha.ToString("yyyyMMdd")))));
        var iva = new XElement(Ns + "Iva");
        foreach (var (rate, id) in new[] { (10.5m, 4), (21m, 5), (27m, 6) })
        {
            var gross = factura.Detalles.Where(x => x.PorcentajeIva == rate).Sum(x => x.Subtotal);
            if (gross == 0) continue;
            var net = FacturaCalculos.Redondear(gross / (1 + rate / 100));
            iva.Add(new XElement(Ns + "AlicIva", E("Id", id), E("BaseImp", M(net)), E("Importe", M(gross - net))));
        }
        if (iva.HasElements) detail.Add(iva);
        return new XElement(Ns + "FeCAEReq", new XElement(Ns + "FeCabReq",
            E("CantReg", 1), E("PtoVta", factura.PuntoVenta), E("CbteTipo", factura.CodigoComprobanteAfip)),
            new XElement(Ns + "FeDetReq", detail)).ToString(SaveOptions.DisableFormatting);
    }
    public static DateTime FechaArgentina() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
        TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "Argentina Standard Time" : "America/Argentina/Buenos_Aires")).Date;
    private static XElement E(string name, object? value) => new(Ns + name, value);
    private static string M(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);
}
