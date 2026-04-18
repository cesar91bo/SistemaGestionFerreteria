using Microsoft.EntityFrameworkCore;
using SistemaGestionFerreteria.Application.Features.Productos.Models;
using SistemaGestionFerreteria.Application.Features.Unidades.Models;
using SistemaGestionFerreteria.Application.Interfaces;
using SistemaGestionFerreteria.Domain.Entities;
using SistemaGestionFerreteria.Infrastructure.Persistence;

namespace SistemaGestionFerreteria.Infrastructure.Services.Unidades
{
    public class UnidadMedidaService : IUnidadMedidaService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public UnidadMedidaService(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<UnidadMedidaViewModel>> ObtenerTodasAsync()
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            return await _context.UnidadesMedida
                .AsNoTracking()
                .OrderBy(x => x.Descripcion)
                .Select(x => new UnidadMedidaViewModel
                {
                    IdUnidadMedida = x.IdUnidadMedida,
                    Descripcion = x.Descripcion,
                    Abreviatura = x.Abreviatura,
                    Activo = x.Activo
                })
                .ToListAsync();
        }

        public async Task CrearAsync(UnidadMedidaViewModel model)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            if (await _context.UnidadesMedida.AnyAsync(x =>
                    x.Descripcion.ToUpper().Trim() == model.Descripcion.ToUpper().Trim()))
            {
                throw new Exception("Ya existe una unidad con esa descripción.");
            }

            var entidad = new UnidadMedida
            {
                Descripcion = model.Descripcion.Trim(),
                Abreviatura = string.IsNullOrWhiteSpace(model.Abreviatura)
                    ? null
                    : model.Abreviatura.Trim(),
                Activo = true
            };

            _context.UnidadesMedida.Add(entidad);

            await _context.SaveChangesAsync();
        }

        public async Task ActualizarAsync(UnidadMedidaViewModel model)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var entidad = await _context.UnidadesMedida
                .FirstOrDefaultAsync(x => x.IdUnidadMedida == model.IdUnidadMedida);

            if (entidad is null)
                throw new Exception("La unidad no existe.");

            if (await _context.UnidadesMedida.AnyAsync(x =>
                    x.IdUnidadMedida != model.IdUnidadMedida &&
                    x.Descripcion.ToUpper().Trim() == model.Descripcion.ToUpper().Trim()))
            {
                throw new Exception("Ya existe otra unidad con esa descripción.");
            }

            entidad.Descripcion = model.Descripcion.Trim();
            entidad.Abreviatura = string.IsNullOrWhiteSpace(model.Abreviatura)
                ? null
                : model.Abreviatura.Trim();

            await _context.SaveChangesAsync();
        }

        public async Task DarDeBajaAsync(int idUnidadMedida)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var entidad = await _context.UnidadesMedida
                .FirstOrDefaultAsync(x => x.IdUnidadMedida == idUnidadMedida);

            if (entidad is null)
                return;

            entidad.Activo = false;

            await _context.SaveChangesAsync();
        }

        public async Task<List<OpcionComboViewModel>> ObtenerComboAsync()
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            return await _context.UnidadesMedida
                .AsNoTracking()
                .Where(x => x.Activo)
                .OrderBy(x => x.Descripcion)
                .Select(x => new OpcionComboViewModel
                {
                    Id = x.IdUnidadMedida,
                    Descripcion = x.Descripcion
                })
                .ToListAsync();
        }
    }
}