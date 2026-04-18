using Microsoft.EntityFrameworkCore;
using SistemaGestionFerreteria.Application.Features.Productos.Models;
using SistemaGestionFerreteria.Application.Features.Proveedores.Models;
using SistemaGestionFerreteria.Application.Interfaces;
using SistemaGestionFerreteria.Domain.Entities;
using SistemaGestionFerreteria.Infrastructure.Persistence;

namespace SistemaGestionFerreteria.Infrastructure.Services
{
    public class ProveedorService : IProveedorService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public ProveedorService(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<OpcionComboViewModel>> ObtenerActivosAsync()
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            return await _context.Proveedores
                .AsNoTracking()
                .Where(x => x.Activo)
                .OrderBy(x => x.Descripcion)
                .Select(x => new OpcionComboViewModel
                {
                    Id = x.IdProveedor,
                    Descripcion = x.Descripcion
                })
                .ToListAsync();
        }

        public async Task<List<ProveedorViewModel>> ObtenerTodosAsync()
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            return await _context.Proveedores
                .AsNoTracking()
                .OrderBy(x => x.Descripcion)
                .Select(x => new ProveedorViewModel
                {
                    IdProveedor = x.IdProveedor,
                    Descripcion = x.Descripcion,
                    Telefono = x.Telefono,
                    Email = x.Email,
                    Direccion = x.Direccion,
                    Activo = x.Activo
                })
                .ToListAsync();
        }

        public async Task<int> CrearAsync(ProveedorViewModel modelo)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            await ValidarDuplicadoAsync(_context, modelo);

            var proveedor = new Proveedor
            {
                Descripcion = modelo.Descripcion.Trim(),
                Telefono = modelo.Telefono?.Trim(),
                Email = modelo.Email?.Trim(),
                Direccion = modelo.Direccion?.Trim(),
                Activo = true
            };

            _context.Proveedores.Add(proveedor);

            await _context.SaveChangesAsync();

            return proveedor.IdProveedor;
        }

        public async Task ActualizarAsync(ProveedorViewModel modelo)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            await ValidarDuplicadoAsync(_context, modelo);

            var proveedor = await _context.Proveedores
                .FirstAsync(x => x.IdProveedor == modelo.IdProveedor);

            proveedor.Descripcion = modelo.Descripcion.Trim();
            proveedor.Telefono = modelo.Telefono?.Trim();
            proveedor.Email = modelo.Email?.Trim();
            proveedor.Direccion = modelo.Direccion?.Trim();

            await _context.SaveChangesAsync();
        }

        public async Task DarDeBajaAsync(int idProveedor)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var proveedor = await _context.Proveedores
                .FirstAsync(x => x.IdProveedor == idProveedor);

            proveedor.Activo = false;

            await _context.SaveChangesAsync();
        }

        public async Task ReactivarAsync(int idProveedor)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var proveedor = await _context.Proveedores
                .FirstAsync(x => x.IdProveedor == idProveedor);

            proveedor.Activo = true;

            await _context.SaveChangesAsync();
        }

        private static async Task ValidarDuplicadoAsync(AppDbContext context, ProveedorViewModel modelo)
        {
            var existe = await context.Proveedores.AnyAsync(x =>
                x.IdProveedor != modelo.IdProveedor &&
                x.Descripcion.ToUpper().Trim() == modelo.Descripcion.ToUpper().Trim());

            if (existe)
            {
                throw new Exception("Ya existe un proveedor con esa descripción.");
            }
        }
    }
}