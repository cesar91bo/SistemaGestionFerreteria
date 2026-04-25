// Domain/Entities/FacturaDetalle.cs
namespace SistemaGestionFerreteria.Domain.Entities
{
    public class FacturaDetalle
    {
        public int IdFacturaDetalle { get; set; }

        public int IdFactura { get; set; }
        public Factura Factura { get; set; } = null!;

        public int IdProducto { get; set; }
        public Producto Producto { get; set; } = null!;

        public string? CodigoProducto { get; set; }
        public string Descripcion { get; set; } = string.Empty;

        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }

        public decimal PorcentajeIva { get; set; }
        public decimal ImporteIva { get; set; }

        public decimal Subtotal { get; set; }
    }
}