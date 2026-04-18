using SistemaGestionFerreteria.Application.Features.Productos.Models;

namespace SistemaGestionFerreteria.Application.Interfaces
{
    public interface IProductoService
    {
        Task<List<ProductoViewModel>> ObtenerTodosAsync();

        Task<int> CrearAsync(ProductoViewModel producto);

        Task ActualizarAsync(ProductoViewModel producto);

        Task DarDeBajaAsync(int idProducto);

        Task ReactivarAsync(int idProducto);

        Task ActualizarPreciosMasivamenteAsync(List<PreviewActualizacionPrecioViewModel> cambios);
    }
}