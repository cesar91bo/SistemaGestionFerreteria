using SistemaGestionFerreteria.Application.Features.Productos.Models;
using SistemaGestionFerreteria.Application.Features.Proveedores.Models;

namespace SistemaGestionFerreteria.Application.Interfaces
{
    public interface IProveedorService
    {
        Task<List<OpcionComboViewModel>> ObtenerActivosAsync();
        Task<List<ProveedorViewModel>> ObtenerTodosAsync();
        Task<int> CrearAsync(ProveedorViewModel modelo);
        Task ActualizarAsync(ProveedorViewModel modelo);
        Task DarDeBajaAsync(int idProveedor);
        Task ReactivarAsync(int idProveedor);
    }
}