using SistemaGestionFerreteria.Application.Features.FacturacionElectronica.Models;

namespace SistemaGestionFerreteria.Application.Interfaces
{
    public interface IAfipWsfeService
    {
        Task<AfipUltimoComprobanteResultado> ObtenerUltimoComprobanteAutorizadoAsync(
            int puntoVenta,
            int codigoTipoComprobanteAfip);
    }
}