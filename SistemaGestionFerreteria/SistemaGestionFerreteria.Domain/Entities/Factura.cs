// Domain/Entities/Factura.cs
using SistemaGestionFerreteria.Domain.Enums;

namespace SistemaGestionFerreteria.Domain.Entities
{
    public class Factura
    {
        public int IdFactura { get; set; }

        public int IdCliente { get; set; }
        public Cliente Cliente { get; set; } = null!;

        public TipoComprobante TipoComprobante { get; set; }
        public int PuntoVenta { get; set; }
        public int NumeroComprobante { get; set; }

        public DateTime Fecha { get; set; }
        public DateTime? FechaVencimiento { get; set; }

        public EstadoFactura Estado { get; set; } = EstadoFactura.Borrador;
        public CondicionVenta CondicionVenta { get; set; }

        public string? Observacion { get; set; }

        public decimal Subtotal { get; set; }
        public decimal TotalIva21 { get; set; }
        public decimal TotalIva105 { get; set; }
        public decimal TotalIva27 { get; set; }
        public decimal TotalExento { get; set; }
        public decimal Total { get; set; }

        public bool EnviadaAfip { get; set; }
        public string? Cae { get; set; }
        public DateTime? FechaVencimientoCae { get; set; }
        public string? ResultadoAfip { get; set; }

        public bool Activo { get; set; } = true;
        public DateTime FechaAlta { get; set; } = DateTime.Now;
        public DateTime? FechaModificacion { get; set; }

        public ICollection<FacturaDetalle> Detalles { get; set; } = new List<FacturaDetalle>();
        public ICollection<FacturaPago> Pagos { get; set; } = new List<FacturaPago>();
    }
}