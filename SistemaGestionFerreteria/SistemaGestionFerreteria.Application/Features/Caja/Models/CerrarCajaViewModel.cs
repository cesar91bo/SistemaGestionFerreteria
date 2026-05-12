namespace SistemaGestionFerreteria.Application.Features.Caja.Models
{
    public class CerrarCajaViewModel
    {
        public int IdCaja { get; set; }

        public decimal MontoContado { get; set; }

        public string? ObservacionCierre { get; set; }
    }
}