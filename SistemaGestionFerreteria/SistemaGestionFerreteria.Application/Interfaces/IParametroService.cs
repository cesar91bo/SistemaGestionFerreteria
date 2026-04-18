namespace SistemaGestionFerreteria.Application.Interfaces
{
    public interface IParametroService
    {
        Task<string?> ObtenerValorAsync(string codigo);
        Task<decimal?> ObtenerDecimalAsync(string codigo);
        Task GuardarAsync(string codigo, string valor, string? grupo = null, string? descripcion = null);
    }
}