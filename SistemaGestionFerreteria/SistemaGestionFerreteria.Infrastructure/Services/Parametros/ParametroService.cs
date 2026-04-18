using Microsoft.EntityFrameworkCore;
using SistemaGestionFerreteria.Application.Interfaces;
using SistemaGestionFerreteria.Domain.Entities;
using SistemaGestionFerreteria.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SistemaGestionFerreteria.Infrastructure.Services.Parametros
{
    public class ParametroService : IParametroService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public ParametroService(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<string?> ObtenerValorAsync(string codigo)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Parametros
                .AsNoTracking()
                .Where(x => x.Activo && x.Codigo == codigo)
                .Select(x => x.Valor)
                .FirstOrDefaultAsync();
        }

        public async Task<decimal?> ObtenerDecimalAsync(string codigo)
        {
            var valor = await ObtenerValorAsync(codigo);

            if (string.IsNullOrWhiteSpace(valor))
                return null;

            if (decimal.TryParse(valor, out var numero))
                return numero;

            return null;
        }

        public async Task GuardarAsync(string codigo, string valor, string? grupo = null, string? descripcion = null)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var parametro = await context.Parametros
                .FirstOrDefaultAsync(x => x.Codigo == codigo);

            if (parametro == null)
            {
                parametro = new Parametro
                {
                    Codigo = codigo,
                    Valor = valor,
                    Grupo = grupo,
                    Descripcion = descripcion,
                    Tipo = Domain.Enums.TipoParametro.Numero,
                    FechaCreacion = DateTime.Now,
                    Activo = true
                };

                context.Parametros.Add(parametro);
            }
            else
            {
                parametro.Valor = valor;
                parametro.Grupo = grupo;
                parametro.Descripcion = descripcion;
                parametro.FechaModificacion = DateTime.Now;
            }

            await context.SaveChangesAsync();
        }
    }
}
