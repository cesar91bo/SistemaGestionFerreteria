// Application/Features/Facturacion/Models/FacturaListadoViewModel.cs
using SistemaGestionFerreteria.Domain.Enums;

namespace SistemaGestionFerreteria.Application.Features.Facturacion.Models
{
    public class FacturaListadoViewModel
    {
        public int IdFactura { get; set; }

        public DateTime Fecha { get; set; }

        public TipoComprobante TipoComprobante { get; set; }

        public int PuntoVenta { get; set; }

        public int NumeroComprobante { get; set; }

        public string Cliente { get; set; } = string.Empty;

        public decimal Total { get; set; }

        public EstadoFactura Estado { get; set; }
    }
}