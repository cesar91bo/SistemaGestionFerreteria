using SistemaGestionFerreteria.Domain.Enums;

namespace SistemaGestionFerreteria.Application.Features.Caja.Models
{
    public class CajaMovimientoViewModel
    {
        public int IdCajaMovimiento { get; set; }

        public int IdCaja { get; set; }

        public int? IdFactura { get; set; }

        public DateTime Fecha { get; set; }

        public TipoMovimientoCaja Tipo { get; set; }

        public decimal Monto { get; set; }

        public string Descripcion { get; set; } = string.Empty;
    }
}