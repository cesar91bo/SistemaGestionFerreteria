// Application/Features/Facturacion/Models/FacturaViewModel.cs
using SistemaGestionFerreteria.Domain.Enums;

namespace SistemaGestionFerreteria.Application.Features.Facturacion.Models
{
    public class FacturaViewModel
    {
        public int IdFactura { get; set; }

        public int? IdCliente { get; set; }

        public TipoComprobante TipoComprobante { get; set; }

        public int PuntoVenta { get; set; } = 1;

        public int NumeroComprobante { get; set; }

        public CondicionVenta CondicionVenta { get; set; }

        public string? Observacion { get; set; }

        public decimal Subtotal { get; set; }

        public decimal TotalIva21 { get; set; }

        public decimal TotalIva105 { get; set; }

        public decimal TotalIva27 { get; set; }

        public decimal TotalExento { get; set; }

        public decimal Total { get; set; }

        public List<FacturaDetalleViewModel> Detalles { get; set; } = new();

        public List<FacturaPagoViewModel> Pagos { get; set; } = new();
    }
}