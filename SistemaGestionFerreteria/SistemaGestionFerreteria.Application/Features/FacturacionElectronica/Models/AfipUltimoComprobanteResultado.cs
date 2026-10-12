namespace SistemaGestionFerreteria.Application.Features.FacturacionElectronica.Models
{
    public class AfipUltimoComprobanteResultado
    {
        public bool Exitoso { get; set; }

        public int PuntoVenta { get; set; }

        public int CodigoTipoComprobanteAfip { get; set; }

        public int UltimoNumero { get; set; }

        public int ProximoNumero => UltimoNumero + 1;

        public string? Error { get; set; }
    }
}