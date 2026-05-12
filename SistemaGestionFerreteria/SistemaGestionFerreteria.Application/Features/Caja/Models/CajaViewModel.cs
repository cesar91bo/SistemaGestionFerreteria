using SistemaGestionFerreteria.Domain.Enums;

namespace SistemaGestionFerreteria.Application.Features.Caja.Models
{
    public class CajaViewModel
    {
        public int IdCaja { get; set; }

        public DateTime FechaApertura { get; set; }

        public DateTime? FechaCierre { get; set; }

        public decimal FondoInicial { get; set; }

        public decimal TotalIngresos { get; set; }

        public decimal TotalRetiros { get; set; }

        public decimal TotalVentas { get; set; }

        public decimal TotalFinal { get; set; }

        public EstadoCaja Estado { get; set; }

        public string? ObservacionApertura { get; set; }

        public string? ObservacionCierre { get; set; }
    }
}