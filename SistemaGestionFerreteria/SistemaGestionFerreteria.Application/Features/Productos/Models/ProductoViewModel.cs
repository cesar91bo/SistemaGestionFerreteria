using System.ComponentModel.DataAnnotations;

namespace SistemaGestionFerreteria.Application.Features.Productos.Models
{
    public class ProductoViewModel
    {
        public int IdProducto { get; set; }

        public string? CodigoProducto { get; set; }

        public string? CodigoBarra { get; set; }

        [Required(ErrorMessage = "La descripción es obligatoria")]
        public string Descripcion { get; set; } = string.Empty;

        public string? Observacion { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una categoría")]
        public int IdCategoria { get; set; }

        public string CategoriaDescripcion { get; set; } = string.Empty;

        public int? IdProveedor { get; set; }

        public string ProveedorDescripcion { get; set; } = string.Empty;

        // Opcional
        public int? IdUnidadMedida { get; set; }

        public string UnidadMedidaDescripcion { get; set; } = string.Empty;

        public bool LlevaStock { get; set; } = true;

        public decimal? StockActual { get; set; }

        public decimal? StockMinimo { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime FechaAlta { get; set; }

        public DateTime? FechaModificacion { get; set; }

        public DateTime? FechaBaja { get; set; }

        public decimal? PrecioVentaActual { get; set; }

        public decimal? PrecioCostoActual { get; set; }

        public DateTime? FechaUltimaActualizacionPrecio { get; set; }

        public bool TienePrecioVigente => PrecioVentaActual.HasValue && PrecioVentaActual.Value > 0;
    }
}