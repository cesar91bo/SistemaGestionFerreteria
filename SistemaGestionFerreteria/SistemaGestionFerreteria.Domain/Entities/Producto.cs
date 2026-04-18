namespace SistemaGestionFerreteria.Domain.Entities
{
    public class Producto
    {
        public int IdProducto { get; set; }

        public string? CodigoProducto { get; set; }

        // Código de barras opcional
        public string? CodigoBarra { get; set; }

        // Nombre principal del producto
        public string Descripcion { get; set; } = string.Empty;

        // Observación o detalle opcional
        public string? Observacion { get; set; }

        public int IdCategoria { get; set; }
        public Categoria? Categoria { get; set; }

        public int? IdProveedor { get; set; }
        public Proveedor? Proveedor { get; set; }
        public int? IdUnidadMedida { get; set; }
        public UnidadMedida? UnidadMedida { get; set; }

        public bool LlevaStock { get; set; } = true;

        // Decimal porque puede haber productos por kg, metro, litro, etc.
        public decimal? StockActual { get; set; }

        public decimal? StockMinimo { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime FechaAlta { get; set; } = DateTime.Now;

        public DateTime? FechaModificacion { get; set; }

        public DateTime? FechaBaja { get; set; }

        public ICollection<ProductoPrecio> Precios { get; set; } = new List<ProductoPrecio>();
    }
}