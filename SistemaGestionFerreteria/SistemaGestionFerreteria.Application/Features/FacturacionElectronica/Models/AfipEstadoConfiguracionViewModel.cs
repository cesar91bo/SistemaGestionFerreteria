namespace SistemaGestionFerreteria.Application.Features.FacturacionElectronica.Models
{
    public class AfipEstadoConfiguracionViewModel
    {
        public bool ConfiguracionValida { get; set; }

        public string Ambiente { get; set; } = string.Empty;

        public string Cuit { get; set; } = string.Empty;

        public int PuntoVenta { get; set; }

        public string Servicio { get; set; } = string.Empty;

        public string WsaaUrl { get; set; } = string.Empty;

        public string WsfeUrl { get; set; } = string.Empty;

        public string ModoCertificado { get; set; } = string.Empty;

        public string? RutaPfx { get; set; }

        public bool ExisteCertificado { get; set; }

        public List<string> Errores { get; set; } = new();
    }
}