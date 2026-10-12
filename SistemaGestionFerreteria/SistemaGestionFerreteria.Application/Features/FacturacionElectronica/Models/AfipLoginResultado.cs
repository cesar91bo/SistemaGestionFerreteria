namespace SistemaGestionFerreteria.Application.Features.FacturacionElectronica.Models
{
    public class AfipLoginResultado
    {
        public bool Exitoso { get; set; }

        public string? Token { get; set; }

        public string? Sign { get; set; }

        public DateTime? ExpirationTime { get; set; }

        public string? Error { get; set; }
    }
}