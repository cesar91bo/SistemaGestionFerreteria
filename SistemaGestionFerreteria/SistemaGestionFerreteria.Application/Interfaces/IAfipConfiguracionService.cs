using SistemaGestionFerreteria.Application.Features.FacturacionElectronica.Models;

namespace SistemaGestionFerreteria.Application.Interfaces
{
    public interface IAfipConfiguracionService
    {
        AfipEstadoConfiguracionViewModel ValidarConfiguracion();
    }
}