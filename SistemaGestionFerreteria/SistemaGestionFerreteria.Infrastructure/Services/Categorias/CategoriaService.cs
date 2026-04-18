using Microsoft.EntityFrameworkCore;
using SistemaGestionFerreteria.Application.Features.Categorias.Models;
using SistemaGestionFerreteria.Application.Features.Productos.Models;
using SistemaGestionFerreteria.Application.Interfaces;
using SistemaGestionFerreteria.Domain.Entities;
using SistemaGestionFerreteria.Infrastructure.Persistence;

namespace SistemaGestionFerreteria.Infrastructure.Services.Categorias
{
    public class CategoriaService : ICategoriaService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public CategoriaService(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<CategoriaViewModel>> ObtenerTodasAsync()
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            return await _context.Categorias
                .AsNoTracking()
                .OrderBy(x => x.Descripcion)
                .Select(x => new CategoriaViewModel
                {
                    IdCategoria = x.IdCategoria,
                    Descripcion = x.Descripcion,
                    Activo = x.Activo
                })
                .ToListAsync();
        }

        public async Task<List<OpcionComboViewModel>> ObtenerActivasAsync()
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            return await _context.Categorias
                .AsNoTracking()
                .Where(x => x.Activo)
                .OrderBy(x => x.Descripcion)
                .Select(x => new OpcionComboViewModel
                {
                    Id = x.IdCategoria,
                    Descripcion = x.Descripcion
                })
                .ToListAsync();
        }

        public async Task CrearAsync(CategoriaViewModel modelo)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var entidad = new Categoria
            {
                Descripcion = modelo.Descripcion.Trim(),
                Activo = true
            };

            _context.Categorias.Add(entidad);

            await _context.SaveChangesAsync();
        }

        public async Task ActualizarAsync(CategoriaViewModel modelo)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var entidad = await _context.Categorias
                .FirstAsync(x => x.IdCategoria == modelo.IdCategoria);

            entidad.Descripcion = modelo.Descripcion.Trim();
            entidad.Activo = true;

            await _context.SaveChangesAsync();
        }

        public async Task DarDeBajaAsync(int id)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var entidad = await _context.Categorias
                .FirstAsync(x => x.IdCategoria == id);

            entidad.Activo = false;

            await _context.SaveChangesAsync();
        }
    }
}