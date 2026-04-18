using Microsoft.EntityFrameworkCore;
using SistemaGestionFerreteria.Application.Features.Productos.Models;
using SistemaGestionFerreteria.Application.Interfaces;
using SistemaGestionFerreteria.Domain.Entities;
using SistemaGestionFerreteria.Domain.Enums;
using SistemaGestionFerreteria.Infrastructure.Persistence;

namespace SistemaGestionFerreteria.Infrastructure.Services.Productos
{
    public class ProductoService : IProductoService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public ProductoService(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<ProductoViewModel>> ObtenerTodosAsync()
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            return await _context.Productos
                .AsNoTracking()
                .Include(x => x.Categoria)
                .Include(x => x.Proveedor)
                .OrderBy(x => x.Descripcion)
                .Select(x => new ProductoViewModel
                {
                    IdProducto = x.IdProducto,
                    CodigoProducto = x.CodigoProducto,
                    CodigoBarra = x.CodigoBarra,
                    Descripcion = x.Descripcion,
                    Observacion = x.Observacion,

                    IdCategoria = x.IdCategoria,
                    CategoriaDescripcion = x.Categoria != null
                        ? x.Categoria.Descripcion
                        : string.Empty,

                    IdProveedor = x.IdProveedor,
                    ProveedorDescripcion = x.Proveedor != null
                        ? x.Proveedor.Descripcion
                        : string.Empty,

                    IdUnidadMedida = x.IdUnidadMedida,

                    LlevaStock = x.LlevaStock,
                    StockActual = x.StockActual,
                    StockMinimo = x.StockMinimo,

                    Activo = x.Activo,

                    FechaAlta = x.FechaAlta,
                    FechaModificacion = x.FechaModificacion,
                    FechaBaja = x.FechaBaja,

                    PrecioCostoActual = x.Precios
                        .Where(p => p.FechaHasta == null)
                        .OrderByDescending(p => p.FechaDesde)
                        .Select(p => (decimal?)p.PrecioCosto)
                        .FirstOrDefault(),

                                        PrecioVentaActual = x.Precios
                        .Where(p => p.FechaHasta == null)
                        .OrderByDescending(p => p.FechaDesde)
                        .Select(p => (decimal?)p.PrecioVenta)
                        .FirstOrDefault(),

                                        FechaUltimaActualizacionPrecio = x.Precios
                        .Where(p => p.FechaHasta == null)
                        .OrderByDescending(p => p.FechaDesde)
                        .Select(p => p.FechaDesde)
                        .FirstOrDefault(),
                        })
                        .ToListAsync();
        }

        public async Task<int> CrearAsync(ProductoViewModel producto)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var descripcion = producto.Descripcion.Trim();

            var existe = await _context.Productos.AnyAsync(x =>
                x.IdCategoria == producto.IdCategoria &&
                x.Descripcion.ToLower() == descripcion.ToLower());

            if (existe)
                throw new Exception("Ya existe un producto con esa descripción dentro de la categoría seleccionada.");

            var entidad = new Producto
            {
                CodigoProducto = string.IsNullOrWhiteSpace(producto.CodigoProducto)
                    ? null
                    : producto.CodigoProducto.Trim(),

                CodigoBarra = producto.CodigoBarra,
                Descripcion = descripcion,
                Observacion = producto.Observacion,

                IdCategoria = producto.IdCategoria,
                IdProveedor = producto.IdProveedor,
                IdUnidadMedida = producto.IdUnidadMedida,

                LlevaStock = producto.LlevaStock,
                StockActual = producto.StockActual,
                StockMinimo = producto.StockMinimo,

                Activo = true,
                FechaAlta = DateTime.Now
            };

            _context.Productos.Add(entidad);

            await _context.SaveChangesAsync();

            return entidad.IdProducto;
        }

        public async Task ActualizarAsync(ProductoViewModel producto)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var entidad = await _context.Productos
                .FirstOrDefaultAsync(x => x.IdProducto == producto.IdProducto);

            if (entidad == null)
                return;

            var descripcion = producto.Descripcion.Trim();

            var existe = await _context.Productos.AnyAsync(x =>
                x.IdProducto != producto.IdProducto &&
                x.IdCategoria == producto.IdCategoria &&
                x.Descripcion.ToLower() == descripcion.ToLower());

            if (existe)
                throw new Exception("Ya existe un producto con esa descripción dentro de la categoría seleccionada.");

            entidad.CodigoProducto = string.IsNullOrWhiteSpace(producto.CodigoProducto)
                ? null
                : producto.CodigoProducto.Trim();

            entidad.CodigoBarra = producto.CodigoBarra;
            entidad.Descripcion = descripcion;
            entidad.Observacion = producto.Observacion;

            entidad.IdCategoria = producto.IdCategoria;
            entidad.IdProveedor = producto.IdProveedor;
            entidad.IdUnidadMedida = producto.IdUnidadMedida;

            entidad.LlevaStock = producto.LlevaStock;
            entidad.StockActual = producto.StockActual;
            entidad.StockMinimo = producto.StockMinimo;

            entidad.FechaModificacion = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        public async Task DarDeBajaAsync(int idProducto)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var entidad = await _context.Productos
                .FirstOrDefaultAsync(x => x.IdProducto == idProducto);

            if (entidad == null)
                return;

            entidad.Activo = false;
            entidad.FechaBaja = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        public async Task ReactivarAsync(int idProducto)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var entidad = await _context.Productos
                .FirstOrDefaultAsync(x => x.IdProducto == idProducto);

            if (entidad == null)
                return;

            entidad.Activo = true;
            entidad.FechaBaja = null;
            entidad.FechaModificacion = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        public async Task ActualizarPreciosMasivamenteAsync(List<PreviewActualizacionPrecioViewModel> cambios)
        {
            if (cambios == null || !cambios.Any())
                return;

            await using var context = await _contextFactory.CreateDbContextAsync();

            var idsProductos = cambios
                .Select(x => x.IdProducto)
                .Distinct()
                .ToList();

            var preciosVigentes = await context.ProductosPrecios
                .Where(x => idsProductos.Contains(x.IdProducto)
                            && x.FechaHasta == null)
                .ToListAsync();

            var fechaActual = DateTime.Now;

            foreach (var cambio in cambios)
            {
                var precioVigente = preciosVigentes
                    .FirstOrDefault(x => x.IdProducto == cambio.IdProducto);

                decimal? precioCosto = precioVigente?.PrecioCosto;
                decimal? porcentajeGanancia = null;
                decimal? precioEspecial = precioVigente?.PrecioEspecial;
                var tipoIva = precioVigente?.TipoIva ?? TipoIvaProducto.Iva21;

                if (precioVigente != null)
                {
                    precioVigente.FechaHasta = fechaActual;

                    if (precioCosto.HasValue && precioCosto.Value > 0)
                    {
                        porcentajeGanancia = Math.Round(
                            ((cambio.PrecioNuevo - precioCosto.Value) / precioCosto.Value) * 100,
                            2);
                    }
                }

                var nuevoPrecio = new ProductoPrecio
                {
                    IdProducto = cambio.IdProducto,
                    PrecioCosto = precioCosto,
                    PrecioVenta = cambio.PrecioNuevo,
                    PorcentajeGanancia = porcentajeGanancia,
                    PrecioEspecial = precioEspecial,
                    TipoIva = tipoIva,
                    FechaDesde = fechaActual,
                    FechaHasta = null,
                    UsuarioAlta = "Sistema"
                };

                context.ProductosPrecios.Add(nuevoPrecio);
            }

            await context.SaveChangesAsync();
        }
    }
}