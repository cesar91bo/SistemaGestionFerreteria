namespace SistemaGestionFerreteria.Application.Features.FacturacionElectronica.Models
{
    public class AfipOptions
    {
        public string Ambiente { get; set; } = "Homologacion";

        public string Cuit { get; set; } = string.Empty;

        public string RazonSocial { get; set; } = string.Empty;

        public string NombreFantasia { get; set; } = string.Empty;

        public string CondicionIva { get; set; } = string.Empty;

        public string DomicilioComercial { get; set; } = string.Empty;

        public string InicioActividades { get; set; } = string.Empty;

        public string? IngresosBrutos { get; set; }

        public int PuntoVenta { get; set; }

        public string Servicio { get; set; } = "wsfe";

        public string WsaaUrl { get; set; } = string.Empty;

        public string WsfeUrl { get; set; } = string.Empty;

        public AfipCertificadoOptions Certificado { get; set; } = new();
    }

    public class AfipCertificadoOptions
    {
        public string Modo { get; set; } = "Store";

        public string? Serial { get; set; }

        public string? RutaPfx { get; set; }

        public string? PasswordPfx { get; set; }
    }
}