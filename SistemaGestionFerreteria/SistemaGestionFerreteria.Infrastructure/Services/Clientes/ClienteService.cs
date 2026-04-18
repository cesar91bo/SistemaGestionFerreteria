using Microsoft.EntityFrameworkCore;
using SistemaGestionFerreteria.Application.Features.Clientes.Models;
using SistemaGestionFerreteria.Application.Features.Productos.Models;
using SistemaGestionFerreteria.Application.Interfaces;
using SistemaGestionFerreteria.Domain.Entities;
using SistemaGestionFerreteria.Infrastructure.Persistence;

namespace SistemaGestionFerreteria.Infrastructure.Services.Clientes
{
    public class ClienteService : IClienteService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public ClienteService(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<OpcionComboViewModel>> ObtenerActivosAsync()
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            return await _context.Clientes
                .AsNoTracking()
                .Where(x => x.Activo)
                .OrderBy(x => x.Nombre)
                .Select(x => new OpcionComboViewModel
                {
                    Id = x.IdCliente,
                    Descripcion = (x.Nombre + " " + (x.Apellido ?? string.Empty)).Trim()
                })
                .ToListAsync();
        }

        public async Task<List<ClienteViewModel>> ObtenerTodosAsync()
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            return await _context.Clientes
                .AsNoTracking()
                .OrderBy(x => x.Nombre)
                .Select(x => new ClienteViewModel
                {
                    IdCliente = x.IdCliente,
                    Nombre = x.Nombre,
                    Apellido = x.Apellido,
                    Documento = x.Documento,
                    RequiereFactura = x.RequiereFactura,
                    Cuit = x.Cuit,
                    RegimenImpositivo = x.RegimenImpositivo,
                    Telefono = x.Telefono,
                    Email = x.Email,
                    Direccion = x.Direccion,
                    Activo = x.Activo
                })
                .ToListAsync();
        }

        public async Task<int> CrearAsync(ClienteViewModel modelo)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            await ValidarDuplicadoAsync(_context, modelo);

            // Normalizar CUIT: solo dígitos
            var normalizedCuit = string.IsNullOrWhiteSpace(modelo.Cuit)
                ? null
                : new string(modelo.Cuit.Where(char.IsDigit).ToArray());

            var entidad = new Cliente
            {
                Nombre = modelo.Nombre.Trim(),
                Apellido = modelo.Apellido?.Trim(),
                Documento = modelo.Documento?.Trim(),
                // Guardar CUIT solo si el usuario indicó que requiere factura
                Cuit = modelo.RequiereFactura ? normalizedCuit : null,
                RequiereFactura = modelo.RequiereFactura,
                RegimenImpositivo = modelo.RegimenImpositivo,
                Telefono = modelo.Telefono?.Trim(),
                Email = modelo.Email?.Trim(),
                Direccion = modelo.Direccion?.Trim(),
                Activo = true
            };

            _context.Clientes.Add(entidad);

            await _context.SaveChangesAsync();

            return entidad.IdCliente;
        }

        public async Task ActualizarAsync(ClienteViewModel modelo)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            await ValidarDuplicadoAsync(_context, modelo);

            var entidad = await _context.Clientes
                .FirstAsync(x => x.IdCliente == modelo.IdCliente);

            entidad.Nombre = modelo.Nombre.Trim();
            entidad.Apellido = modelo.Apellido?.Trim();

            // Documento siempre viene del modelo
            entidad.Documento = modelo.Documento?.Trim();

            // Normalizar CUIT y guardar sólo si el usuario indicó que requiere factura
            var normalizedCuit = string.IsNullOrWhiteSpace(modelo.Cuit)
                ? null
                : new string(modelo.Cuit.Where(char.IsDigit).ToArray());

            entidad.Cuit = modelo.RequiereFactura ? normalizedCuit : null;

            // Guardar el flag tal cual lo indicó el usuario
            entidad.RequiereFactura = modelo.RequiereFactura;

            entidad.RegimenImpositivo = modelo.RegimenImpositivo;
            entidad.Telefono = modelo.Telefono?.Trim();
            entidad.Email = modelo.Email?.Trim();
            entidad.Direccion = modelo.Direccion?.Trim();

            await _context.SaveChangesAsync();
        }

        public async Task DarDeBajaAsync(int idCliente)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var entidad = await _context.Clientes
                .FirstAsync(x => x.IdCliente == idCliente);

            entidad.Activo = false;

            await _context.SaveChangesAsync();
        }

        public async Task ReactivarAsync(int idCliente)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var entidad = await _context.Clientes
                .FirstAsync(x => x.IdCliente == idCliente);

            entidad.Activo = true;

            await _context.SaveChangesAsync();
        }

        private static async Task ValidarDuplicadoAsync(AppDbContext context, ClienteViewModel modelo)
        {
            // Si se proporciona CUIT, validar unicidad del CUIT
            if (!string.IsNullOrWhiteSpace(modelo.Cuit))
            {
                var existeCuit = await context.Clientes.AnyAsync(x =>
                    x.IdCliente != modelo.IdCliente &&
                    x.Cuit != null && x.Cuit.ToUpper().Trim() == modelo.Cuit.ToUpper().Trim());

                if (existeCuit)
                {
                    throw new Exception("Ya existe un cliente con ese CUIT.");
                }
            }

            var existe = !string.IsNullOrWhiteSpace(modelo.Documento)
                ? await context.Clientes.AnyAsync(x =>
                    x.IdCliente != modelo.IdCliente &&
                    x.Documento.ToUpper().Trim() == modelo.Documento.ToUpper().Trim())
                : await context.Clientes.AnyAsync(x =>
                    x.IdCliente != modelo.IdCliente &&
                    x.Nombre.ToUpper().Trim() == modelo.Nombre.ToUpper().Trim() &&
                    (x.Apellido ?? string.Empty).ToUpper().Trim() == (modelo.Apellido ?? string.Empty).ToUpper().Trim());

            if (existe)
            {
                throw new Exception("Ya existe un cliente con esos datos.");
            }
        }
    }
}
