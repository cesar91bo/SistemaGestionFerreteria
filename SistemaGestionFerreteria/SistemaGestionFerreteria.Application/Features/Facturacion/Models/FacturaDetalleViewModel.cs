// Application/Features/Facturacion/Models/FacturaDetalleViewModel.cs
namespace SistemaGestionFerreteria.Application.Features.Facturacion.Models
{
    public class FacturaDetalleViewModel
    {
        public int? IdProducto { get; set; }

        public string? CodigoProducto { get; set; }

        public string Descripcion { get; set; } = string.Empty;

        public decimal Cantidad { get; set; }

        public decimal PrecioUnitario { get; set; }

        public decimal PorcentajeIva { get; set; }

        public decimal ImporteIva { get; set; }

        public decimal Subtotal { get; set; }
    }
}