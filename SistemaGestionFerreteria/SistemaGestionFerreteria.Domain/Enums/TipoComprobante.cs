using System.ComponentModel.DataAnnotations;

namespace SistemaGestionFerreteria.Domain.Enums
{
    public enum TipoComprobante
    {
        [Display(Name = "Presupuesto")]
        Presupuesto = 0,

        [Display(Name = "Factura A")]
        FacturaA = 1,

        [Display(Name = "Factura B")]
        FacturaB = 2,

        [Display(Name = "Factura C")]
        FacturaC = 3,

        [Display(Name = "Remito")]
        Remito = 4,

        [Display(Name = "Nota de crédito")]
        NotaCredito = 5
    }
}