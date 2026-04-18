using System.ComponentModel.DataAnnotations;
using SistemaGestionFerreteria.Domain.Enums;

namespace SistemaGestionFerreteria.Application.Features.Productos.Models
{
    public class ProductoPrecioEdicionViewModel
    {
        public int IdProducto { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "El precio costo no puede ser negativo")]
        public decimal? PrecioCosto { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "El precio final debe ser mayor a 0")]
        public decimal PrecioVenta { get; set; }

        [Range(0, 10000, ErrorMessage = "El porcentaje de ganancia no es válido")]
        public decimal? PorcentajeGanancia { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "El precio especial no puede ser negativo")]
        public decimal? PrecioEspecial { get; set; }

        public TipoIvaProducto TipoIva { get; set; } = TipoIvaProducto.Iva21;

        public decimal PorcentajeIvaValor
        {
            get
            {
                return TipoIva switch
                {
                    TipoIvaProducto.Exento => 0m,
                    TipoIvaProducto.Iva105 => 10.5m,
                    TipoIvaProducto.Iva21 => 21m,
                    TipoIvaProducto.Iva27 => 27m,
                    _ => 21m
                };
            }
        }

        public DateTime? FechaDesdePrecioVigente { get; set; }

        public CampoPrecioEditado UltimoCampoEditado { get; set; } = CampoPrecioEditado.PrecioCosto;
    }
}