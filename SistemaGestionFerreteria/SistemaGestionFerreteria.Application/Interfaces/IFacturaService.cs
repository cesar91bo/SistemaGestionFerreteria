// Application/Interfaces/IFacturaService.cs
using SistemaGestionFerreteria.Application.Features.Facturacion.Models;
using SistemaGestionFerreteria.Domain.Enums;

namespace SistemaGestionFerreteria.Application.Interfaces
{
    public interface IFacturaService
    {
        Task<int> CrearAsync(FacturaViewModel model);

        Task<List<FacturaListadoViewModel>> ObtenerTodasAsync();

        Task<FacturaViewModel?> ObtenerPorIdAsync(int idFactura);

        Task<int> ObtenerProximoNumeroAsync(TipoComprobante tipoComprobante, int puntoVenta);
    }
}