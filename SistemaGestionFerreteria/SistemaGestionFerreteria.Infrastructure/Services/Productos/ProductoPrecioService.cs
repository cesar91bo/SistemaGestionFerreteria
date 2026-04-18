using Microsoft.EntityFrameworkCore;
using SistemaGestionFerreteria.Application.Features.Productos.Models;
using SistemaGestionFerreteria.Application.Interfaces;
using SistemaGestionFerreteria.Domain.Entities;
using SistemaGestionFerreteria.Infrastructure.Persistence;

namespace SistemaGestionFerreteria.Infrastructure.Services.Productos
{
    public class ProductoPrecioService : IProductoPrecioService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public ProductoPrecioService(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<ProductoPrecioEdicionViewModel> ObtenerPrecioVigenteAsync(int idProducto)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var precioVigente = await _context.ProductosPrecios
                .AsNoTracking()
                .Where(x => x.IdProducto == idProducto && x.FechaHasta == null)
                .OrderByDescending(x => x.FechaDesde)
                .FirstOrDefaultAsync();

            if (precioVigente == null)
            {
                return new ProductoPrecioEdicionViewModel
                {
                    IdProducto = idProducto
                };
            }

            return new ProductoPrecioEdicionViewModel
            {
                IdProducto = precioVigente.IdProducto,
                PrecioCosto = precioVigente.PrecioCosto,
                PorcentajeGanancia = precioVigente.PorcentajeGanancia,
                PrecioVenta = precioVigente.PrecioVenta,
                PrecioEspecial = precioVigente.PrecioEspecial,
                TipoIva = precioVigente.TipoIva,
                FechaDesdePrecioVigente = precioVigente.FechaDesde
            };
        }

        public async Task<List<ProductoPrecioHistorialViewModel>> ObtenerHistorialAsync(int idProducto)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            return await _context.ProductosPrecios
                .AsNoTracking()
                .Where(x => x.IdProducto == idProducto)
                .OrderByDescending(x => x.FechaDesde)
                .Select(x => new ProductoPrecioHistorialViewModel
                {
                    IdProductoPrecio = x.IdProductoPrecio,
                    IdProducto = x.IdProducto,
                    PrecioCosto = x.PrecioCosto,
                    PorcentajeGanancia = x.PorcentajeGanancia,
                    PrecioVenta = x.PrecioVenta,
                    PrecioEspecial = x.PrecioEspecial,
                    TipoIva = x.TipoIva,
                    FechaDesde = x.FechaDesde,
                    FechaHasta = x.FechaHasta,
                    UsuarioAlta = x.UsuarioAlta
                })
                .ToListAsync();
        }

        public async Task GuardarNuevoPrecioAsync(ProductoPrecioEdicionViewModel modelo, string? usuarioAlta = null)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            if (modelo == null)
                throw new ArgumentNullException(nameof(modelo));

            if (modelo.IdProducto <= 0)
                throw new Exception("El producto no es válido.");

            if (modelo.PrecioVenta <= 0)
                throw new Exception("El precio de venta debe ser mayor a 0.");

            if (modelo.PrecioCosto < 0)
                throw new Exception("El precio de costo no puede ser negativo.");

            var productoExiste = await _context.Productos
                .AsNoTracking()
                .AnyAsync(x => x.IdProducto == modelo.IdProducto);

            if (!productoExiste)
                throw new Exception("No se encontró el producto.");

            var ahora = DateTime.Now;

            using var transaction = await _context.Database.BeginTransactionAsync();

            var preciosVigentes = await _context.ProductosPrecios
                .Where(x => x.IdProducto == modelo.IdProducto && x.FechaHasta == null)
                .ToListAsync();

            var precioVigente = preciosVigentes
                .OrderByDescending(x => x.FechaDesde)
                .FirstOrDefault();

            if (precioVigente != null)
            {
                var mismoPrecio =
                    precioVigente.PrecioCosto == modelo.PrecioCosto &&
                    precioVigente.PorcentajeGanancia == modelo.PorcentajeGanancia &&
                    precioVigente.PrecioVenta == modelo.PrecioVenta &&
                    precioVigente.PrecioEspecial == modelo.PrecioEspecial &&
                    precioVigente.TipoIva == modelo.TipoIva;

                if (mismoPrecio)
                    throw new Exception("El precio ingresado es igual al precio vigente.");
            }

            foreach (var precioAnterior in preciosVigentes)
            {
                precioAnterior.FechaHasta = ahora;
            }

            var nuevoPrecio = new ProductoPrecio
            {
                IdProducto = modelo.IdProducto,
                PrecioCosto = modelo.PrecioCosto,
                PorcentajeGanancia = modelo.PorcentajeGanancia,
                PrecioVenta = modelo.PrecioVenta,
                PrecioEspecial = modelo.PrecioEspecial,
                TipoIva = modelo.TipoIva,
                FechaDesde = ahora,
                FechaHasta = null,
                UsuarioAlta = usuarioAlta
            };

            _context.ProductosPrecios.Add(nuevoPrecio);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var detalle = ex.InnerException?.Message
                              ?? ex.GetBaseException().Message
                              ?? ex.Message;

                throw new Exception(detalle);
            }
            await transaction.CommitAsync();
        }
    }
}