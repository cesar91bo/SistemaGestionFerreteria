using Microsoft.EntityFrameworkCore;
using SistemaGestionFerreteria.Application.Features.Caja.Models;
using SistemaGestionFerreteria.Application.Interfaces;
using SistemaGestionFerreteria.Domain.Entities;
using SistemaGestionFerreteria.Domain.Enums;
using SistemaGestionFerreteria.Infrastructure.Persistence;

namespace SistemaGestionFerreteria.Infrastructure.Services.Caja
{
    public class CajaService : ICajaService
    {
        private readonly AppDbContext _context;

        public CajaService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> HayCajaAbiertaAsync()
        {
            return await _context.Cajas
                .AnyAsync(x => x.Activo && x.Estado == EstadoCaja.Abierta);
        }

        public async Task<CajaViewModel?> ObtenerCajaAbiertaAsync()
        {
            return await _context.Cajas
                .AsNoTracking()
                .Where(x => x.Activo && x.Estado == EstadoCaja.Abierta)
                .Select(x => new CajaViewModel
                {
                    IdCaja = x.IdCaja,
                    FechaApertura = x.FechaApertura,
                    FechaCierre = x.FechaCierre,
                    FondoInicial = x.FondoInicial,
                    TotalIngresos = x.TotalIngresos,
                    TotalRetiros = x.TotalRetiros,
                    TotalVentas = x.TotalVentas,
                    TotalFinal = x.TotalFinal,
                    Estado = x.Estado,
                    ObservacionApertura = x.ObservacionApertura,
                    ObservacionCierre = x.ObservacionCierre
                })
                .FirstOrDefaultAsync();
        }

        public async Task<List<CajaMovimientoViewModel>> ObtenerMovimientosAsync(int idCaja)
        {
            return await _context.CajaMovimientos
                .AsNoTracking()
                .Where(x => x.Activo && x.IdCaja == idCaja)
                .OrderByDescending(x => x.Fecha)
                .Select(x => new CajaMovimientoViewModel
                {
                    IdCajaMovimiento = x.IdCajaMovimiento,
                    IdCaja = x.IdCaja,
                    IdFactura = x.IdFactura,
                    Fecha = x.Fecha,
                    Tipo = x.Tipo,
                    Monto = x.Monto,
                    Descripcion = x.Descripcion
                })
                .ToListAsync();
        }

        public async Task AbrirCajaAsync(AbrirCajaViewModel modelo)
        {
            var hayCajaAbierta = await HayCajaAbiertaAsync();

            if (hayCajaAbierta)
                throw new InvalidOperationException("Ya existe una caja abierta.");

            if (modelo.FondoInicial < 0)
                throw new InvalidOperationException("El fondo inicial no puede ser negativo.");

            var caja = new Domain.Entities.Caja
            {
                FechaApertura = DateTime.Now,
                FondoInicial = modelo.FondoInicial,
                TotalIngresos = 0,
                TotalRetiros = 0,
                TotalVentas = 0,
                TotalFinal = modelo.FondoInicial,
                Estado = EstadoCaja.Abierta,
                ObservacionApertura = modelo.ObservacionApertura,
                Activo = true,
                FechaAlta = DateTime.Now
            };

            _context.Cajas.Add(caja);
            await _context.SaveChangesAsync();

            var movimiento = new CajaMovimiento
            {
                IdCaja = caja.IdCaja,
                Fecha = DateTime.Now,
                Tipo = TipoMovimientoCaja.Ingreso,
                Monto = modelo.FondoInicial,
                Descripcion = "Apertura de caja",
                Activo = true
            };

            _context.CajaMovimientos.Add(movimiento);
            await _context.SaveChangesAsync();
        }

        public async Task CerrarCajaAsync(CerrarCajaViewModel modelo)
        {
            var caja = await _context.Cajas
                .FirstOrDefaultAsync(x =>
                    x.Activo &&
                    x.IdCaja == modelo.IdCaja &&
                    x.Estado == EstadoCaja.Abierta);

            if (caja == null)
                throw new InvalidOperationException("No se encontró una caja abierta para cerrar.");

            if (modelo.MontoContado < 0)
                throw new InvalidOperationException("El monto contado no puede ser negativo.");

            caja.FechaCierre = DateTime.Now;
            caja.TotalFinal = modelo.MontoContado;
            caja.ObservacionCierre = modelo.ObservacionCierre;
            caja.Estado = EstadoCaja.Cerrada;
            caja.FechaModificacion = DateTime.Now;

            var movimiento = new CajaMovimiento
            {
                IdCaja = caja.IdCaja,
                Fecha = DateTime.Now,
                Tipo = TipoMovimientoCaja.Retiro,
                Monto = modelo.MontoContado,
                Descripcion = "Cierre de caja",
                Activo = true
            };

            _context.CajaMovimientos.Add(movimiento);

            await _context.SaveChangesAsync();
        }

        public async Task RegistrarIngresoAsync(int idCaja, decimal monto, string descripcion)
        {
            if (monto <= 0)
                throw new InvalidOperationException("El monto del ingreso debe ser mayor a cero.");

            var caja = await ObtenerCajaEntidadAbiertaAsync(idCaja);

            caja.TotalIngresos += monto;
            caja.TotalFinal = caja.FondoInicial + caja.TotalIngresos + caja.TotalVentas - caja.TotalRetiros;
            caja.FechaModificacion = DateTime.Now;

            var movimiento = new CajaMovimiento
            {
                IdCaja = caja.IdCaja,
                Fecha = DateTime.Now,
                Tipo = TipoMovimientoCaja.Ingreso,
                Monto = monto,
                Descripcion = descripcion,
                Activo = true
            };

            _context.CajaMovimientos.Add(movimiento);

            await _context.SaveChangesAsync();
        }

        public async Task RegistrarRetiroAsync(int idCaja, decimal monto, string descripcion)
        {
            if (monto <= 0)
                throw new InvalidOperationException("El monto del retiro debe ser mayor a cero.");

            var caja = await ObtenerCajaEntidadAbiertaAsync(idCaja);

            var disponible = caja.FondoInicial + caja.TotalIngresos + caja.TotalVentas - caja.TotalRetiros;

            if (monto > disponible)
                throw new InvalidOperationException("El retiro no puede ser mayor al dinero disponible en caja.");

            caja.TotalRetiros += monto;
            caja.TotalFinal = caja.FondoInicial + caja.TotalIngresos + caja.TotalVentas - caja.TotalRetiros;
            caja.FechaModificacion = DateTime.Now;

            var movimiento = new CajaMovimiento
            {
                IdCaja = caja.IdCaja,
                Fecha = DateTime.Now,
                Tipo = TipoMovimientoCaja.Retiro,
                Monto = monto,
                Descripcion = descripcion,
                Activo = true
            };

            _context.CajaMovimientos.Add(movimiento);

            await _context.SaveChangesAsync();
        }

        public async Task RegistrarVentaAsync(int idCaja, int idFactura, decimal monto)
        {
            if (monto <= 0)
                throw new InvalidOperationException("El monto de la venta debe ser mayor a cero.");

            var caja = await ObtenerCajaEntidadAbiertaAsync(idCaja);

            caja.TotalVentas += monto;
            caja.TotalFinal = caja.FondoInicial + caja.TotalIngresos + caja.TotalVentas - caja.TotalRetiros;
            caja.FechaModificacion = DateTime.Now;

            var movimiento = new CajaMovimiento
            {
                IdCaja = caja.IdCaja,
                IdFactura = idFactura,
                Fecha = DateTime.Now,
                Tipo = TipoMovimientoCaja.Venta,
                Monto = monto,
                Descripcion = $"Venta comprobante #{idFactura}",
                Activo = true
            };

            _context.CajaMovimientos.Add(movimiento);

            await _context.SaveChangesAsync();
        }

        private async Task<Domain.Entities.Caja> ObtenerCajaEntidadAbiertaAsync(int idCaja)
        {
            var caja = await _context.Cajas
                .FirstOrDefaultAsync(x =>
                    x.Activo &&
                    x.IdCaja == idCaja &&
                    x.Estado == EstadoCaja.Abierta);

            if (caja == null)
                throw new InvalidOperationException("No se encontró una caja abierta.");

            return caja;
        }

        public async Task RegistrarAnulacionVentaAsync(int idCaja, int idFactura, decimal monto)
        {
            if (monto <= 0)
                throw new InvalidOperationException("El monto de la anulación debe ser mayor a cero.");

            var caja = await ObtenerCajaEntidadAbiertaAsync(idCaja);

            if (monto > caja.TotalVentas)
                throw new InvalidOperationException("La anulación no puede ser mayor al total de ventas registradas en caja.");

            caja.TotalVentas -= monto;
            caja.TotalFinal = caja.FondoInicial + caja.TotalIngresos + caja.TotalVentas - caja.TotalRetiros;
            caja.FechaModificacion = DateTime.Now;

            var movimiento = new CajaMovimiento
            {
                IdCaja = caja.IdCaja,
                IdFactura = idFactura,
                Fecha = DateTime.Now,
                Tipo = TipoMovimientoCaja.AnulacionVenta,
                Monto = monto,
                Descripcion = $"Anulación comprobante #{idFactura}",
                Activo = true
            };

            _context.CajaMovimientos.Add(movimiento);

            await _context.SaveChangesAsync();
        }

        public async Task<List<CajaViewModel>> ObtenerCajasCerradasAsync()
        {
            return await _context.Cajas
                .AsNoTracking()
                .Where(x => x.Activo && x.Estado == EstadoCaja.Cerrada)
                .OrderByDescending(x => x.FechaCierre)
                .Select(x => new CajaViewModel
                {
                    IdCaja = x.IdCaja,
                    FechaApertura = x.FechaApertura,
                    FechaCierre = x.FechaCierre,
                    FondoInicial = x.FondoInicial,
                    TotalIngresos = x.TotalIngresos,
                    TotalRetiros = x.TotalRetiros,
                    TotalVentas = x.TotalVentas,
                    TotalFinal = x.TotalFinal,
                    Estado = x.Estado,
                    ObservacionApertura = x.ObservacionApertura,
                    ObservacionCierre = x.ObservacionCierre
                })
                .ToListAsync();
        }
    }
}