using SistemaGestionFerreteria.Domain.Enums;

namespace SistemaGestionFerreteria.Domain.Entities
{
    public class ProductoPrecio
    {
        public int IdProductoPrecio { get; set; }

        public int IdProducto { get; set; }
        public Producto? Producto { get; set; }

        // Precio de compra / costo
        public decimal? PrecioCosto { get; set; }

        // Precio normal de venta
        public decimal PrecioVenta { get; set; }

        public decimal? PorcentajeGanancia { get; set; }

        // Opcional para clientes especiales o promociones
        public decimal? PrecioEspecial { get; set; }

        // IVA aplicado en ese momento (21, 10.5, etc.)
        public TipoIvaProducto TipoIva { get; set; } = TipoIvaProducto.Iva21;

        // Desde cuándo estuvo vigente ese precio
        public DateTime FechaDesde { get; set; } = DateTime.Now;

        // Hasta cuándo estuvo vigente.
        // Null = precio actual
        public DateTime? FechaHasta { get; set; }

        public string? UsuarioAlta { get; set; }
    }
}