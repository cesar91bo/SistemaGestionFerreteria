using SistemaGestionFerreteria.Application.Features.Productos.Models;
using SistemaGestionFerreteria.Application.Features.Unidades.Models;

namespace SistemaGestionFerreteria.Application.Interfaces
{
    public interface IUnidadMedidaService
    {
        Task<List<UnidadMedidaViewModel>> ObtenerTodasAsync();

        Task<List<OpcionComboViewModel>> ObtenerComboAsync();

        Task CrearAsync(UnidadMedidaViewModel model);

        Task ActualizarAsync(UnidadMedidaViewModel model);

        Task DarDeBajaAsync(int idUnidadMedida);
    }
}