using SistemaGestionFerreteria.Application.Features.FacturacionElectronica.Models;

namespace SistemaGestionFerreteria.Application.Interfaces
{
    public interface IAfipWsfeService
    {
        Task<AfipAutorizacionResultado> AutorizarAsync(string solicitudXml);
        Task<AfipAutorizacionResultado> ConsultarAsync(string solicitudXml);
        Task<AfipUltimoComprobanteResultado> ObtenerUltimoComprobanteAutorizadoAsync(
            int puntoVenta,
            int codigoTipoComprobanteAfip);
    }
}
