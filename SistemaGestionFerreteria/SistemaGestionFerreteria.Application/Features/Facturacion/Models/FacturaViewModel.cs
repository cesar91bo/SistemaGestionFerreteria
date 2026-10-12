// Application/Features/Facturacion/Models/FacturaViewModel.cs
using SistemaGestionFerreteria.Domain.Enums;

namespace SistemaGestionFerreteria.Application.Features.Facturacion.Models
{
    public class FacturaViewModel
    {
        public int IdFactura { get; set; }

        public int? IdCliente { get; set; }

        public TipoComprobante TipoComprobante { get; set; }

        public int PuntoVenta { get; set; }
        public Guid OperacionId { get; set; } = Guid.NewGuid();
        public bool EnviarAfip { get; set; } = true;
        public bool EnviadaAfip { get; set; }
        public string? Cae { get; set; }
        public DateTime? FechaVencimientoCae { get; set; }
        public string? ResultadoAfip { get; set; }
        public string? ErrorAfip { get; set; }
        public string? AmbienteAfip { get; set; }
        public EstadoFactura Estado { get; set; }
        public int? IdCajaVenta { get; set; }
        public int? IdFacturaAsociada { get; set; }

        public int NumeroComprobante { get; set; }

        public CondicionVenta CondicionVenta { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Today;

        public string? Observacion { get; set; }

        public decimal Subtotal { get; set; }

        public decimal TotalIva21 { get; set; }

        public decimal TotalIva105 { get; set; }

        public decimal TotalIva27 { get; set; }

        public decimal TotalExento { get; set; }

        public decimal Total { get; set; }
        public decimal? MontoRecibido { get; set; }

        public List<FacturaDetalleViewModel> Detalles { get; set; } = new();

        public List<FacturaPagoViewModel> Pagos { get; set; } = new();
    }
}
