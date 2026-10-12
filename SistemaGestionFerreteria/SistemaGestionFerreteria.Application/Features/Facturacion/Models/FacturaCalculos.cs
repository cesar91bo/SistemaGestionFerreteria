namespace SistemaGestionFerreteria.Application.Features.Facturacion.Models;

public static class FacturaCalculos
{
    public static decimal Redondear(decimal importe) => Math.Round(importe, 2, MidpointRounding.AwayFromZero);
    public static void Recalcular(FacturaViewModel model, bool validar = true)
    {
        foreach (var d in model.Detalles)
        {
            if (validar && (d.Cantidad <= 0 || d.PrecioUnitario < 0)) throw new InvalidOperationException("La cantidad debe ser positiva y el precio no puede ser negativo.");
            if (validar && d.PorcentajeIva is not (0m or 10.5m or 21m or 27m)) throw new InvalidOperationException("Alícuota IVA no soportada.");
            d.Cantidad = Redondear(d.Cantidad);
            d.PrecioUnitario = Redondear(d.PrecioUnitario);
            if (validar && d.Cantidad <= 0) throw new InvalidOperationException("La cantidad debe ser al menos 0,01.");
            d.Subtotal = Redondear(d.Cantidad * d.PrecioUnitario);
            d.ImporteIva = d.PorcentajeIva == 0 ? 0 : d.Subtotal - Redondear(d.Subtotal / (1 + d.PorcentajeIva / 100));
        }
        decimal Iva(decimal rate)
        {
            var gross = model.Detalles.Where(x => x.PorcentajeIva == rate).Sum(x => x.Subtotal);
            return gross - Redondear(gross / (1 + rate / 100));
        }
        model.Total = model.Detalles.Sum(x => x.Subtotal);
        model.TotalIva21 = Iva(21);
        model.TotalIva105 = Iva(10.5m);
        model.TotalIva27 = Iva(27);
        model.TotalExento = model.Detalles.Where(x => x.PorcentajeIva == 0).Sum(x => x.Subtotal);
        // Subtotal mostrado incluye exentos; WSFE ImpNeto los excluye.
        model.Subtotal = model.Total - model.TotalIva21 - model.TotalIva105 - model.TotalIva27;
    }
}
