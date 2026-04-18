using SistemaGestionFerreteria.Application.Features.Productos.Models;

namespace SistemaGestionFerreteria.Application.Interfaces
{
    public interface IProductoPrecioService
    {
        Task<ProductoPrecioEdicionViewModel> ObtenerPrecioVigenteAsync(int idProducto);

        Task<List<ProductoPrecioHistorialViewModel>> ObtenerHistorialAsync(int idProducto);

        Task GuardarNuevoPrecioAsync(ProductoPrecioEdicionViewModel modelo, string? usuarioAlta = null);
    }
}