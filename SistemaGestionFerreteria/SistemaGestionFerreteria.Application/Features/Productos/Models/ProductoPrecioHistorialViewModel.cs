using SistemaGestionFerreteria.Domain.Enums;

namespace SistemaGestionFerreteria.Application.Features.Productos.Models
{
    public class ProductoPrecioHistorialViewModel
    {
        public int IdProductoPrecio { get; set; }

        public int IdProducto { get; set; }

        public decimal? PrecioCosto { get; set; }

        public decimal? PorcentajeGanancia { get; set; }

        public decimal PrecioVenta { get; set; }

        public decimal? PrecioEspecial { get; set; }

        public TipoIvaProducto TipoIva { get; set; } = TipoIvaProducto.Iva21;

        public DateTime FechaDesde { get; set; }

        public DateTime? FechaHasta { get; set; }

        public string? UsuarioAlta { get; set; }

        public bool EsVigente => FechaHasta == null;
    }
}