using SistemaGestionFerreteria.Application.Features.Categorias.Models;
using SistemaGestionFerreteria.Application.Features.Productos.Models;

namespace SistemaGestionFerreteria.Application.Interfaces
{
    public interface ICategoriaService
    {
        Task<List<CategoriaViewModel>> ObtenerTodasAsync();

        Task<List<OpcionComboViewModel>> ObtenerActivasAsync();

        Task CrearAsync(CategoriaViewModel modelo);

        Task ActualizarAsync(CategoriaViewModel modelo);

        Task DarDeBajaAsync(int id);
    }
}