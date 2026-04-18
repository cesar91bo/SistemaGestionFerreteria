namespace SistemaGestionFerreteria.Application.Features.Productos.Models
{
    public class PreviewActualizacionPrecioViewModel
    {
        public int IdProducto { get; set; }

        public string Descripcion { get; set; } = string.Empty;

        public decimal PrecioActual { get; set; }

        public decimal PrecioNuevo { get; set; }

        public decimal DiferenciaPorcentaje { get; set; }
    }
}