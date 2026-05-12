using SistemaGestionFerreteria.Domain.Enums;

namespace SistemaGestionFerreteria.Domain.Entities
{
    public class Caja
    {
        public int IdCaja { get; set; }

        public DateTime FechaApertura { get; set; } = DateTime.Now;

        public DateTime? FechaCierre { get; set; }

        public decimal FondoInicial { get; set; }

        public decimal TotalIngresos { get; set; }

        public decimal TotalRetiros { get; set; }

        public decimal TotalVentas { get; set; }

        public decimal TotalFinal { get; set; }

        public EstadoCaja Estado { get; set; } = EstadoCaja.Abierta;

        public string? ObservacionApertura { get; set; }

        public string? ObservacionCierre { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime FechaAlta { get; set; } = DateTime.Now;

        public DateTime? FechaModificacion { get; set; }

        public ICollection<CajaMovimiento> Movimientos { get; set; } = new List<CajaMovimiento>();
    }
}