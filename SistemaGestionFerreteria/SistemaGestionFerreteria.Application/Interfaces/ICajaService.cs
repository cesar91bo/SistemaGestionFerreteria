using SistemaGestionFerreteria.Application.Features.Caja.Models;

namespace SistemaGestionFerreteria.Application.Interfaces
{
    public interface ICajaService
    {
        Task<bool> HayCajaAbiertaAsync();

        Task<CajaViewModel?> ObtenerCajaAbiertaAsync();

        Task<List<CajaMovimientoViewModel>> ObtenerMovimientosAsync(int idCaja);

        Task AbrirCajaAsync(AbrirCajaViewModel modelo);

        Task CerrarCajaAsync(CerrarCajaViewModel modelo);

        Task RegistrarIngresoAsync(int idCaja, decimal monto, string descripcion);

        Task RegistrarRetiroAsync(int idCaja, decimal monto, string descripcion);

        Task RegistrarVentaAsync(int idCaja, int idFactura, decimal monto);

        Task RegistrarAnulacionVentaAsync(int idCaja, int idFactura, decimal monto);

        Task<List<CajaViewModel>> ObtenerCajasCerradasAsync();
    }
}