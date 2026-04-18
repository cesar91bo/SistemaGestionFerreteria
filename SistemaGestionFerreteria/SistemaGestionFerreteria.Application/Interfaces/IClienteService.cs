using SistemaGestionFerreteria.Application.Features.Productos.Models;
using SistemaGestionFerreteria.Application.Features.Clientes.Models;

namespace SistemaGestionFerreteria.Application.Interfaces
{
    public interface IClienteService
    {
        Task<List<OpcionComboViewModel>> ObtenerActivosAsync();
        Task<List<ClienteViewModel>> ObtenerTodosAsync();
        Task<int> CrearAsync(ClienteViewModel modelo);
        Task ActualizarAsync(ClienteViewModel modelo);
        Task DarDeBajaAsync(int idCliente);
        Task ReactivarAsync(int idCliente);
    }
}