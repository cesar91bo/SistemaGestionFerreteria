// Domain/Entities/FacturaPago.cs
using SistemaGestionFerreteria.Domain.Enums;

namespace SistemaGestionFerreteria.Domain.Entities
{
    public class FacturaPago
    {
        public int IdFacturaPago { get; set; }

        public int IdFactura { get; set; }
        public Factura Factura { get; set; } = null!;

        public FormaPago FormaPago { get; set; }

        public decimal Monto { get; set; }

        public string? Referencia { get; set; }
    }
}