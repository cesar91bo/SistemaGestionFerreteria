using SistemaGestionFerreteria.Domain.Enums;

namespace SistemaGestionFerreteria.Domain.Entities
{
    public class Parametro
    {
        public int Id { get; set; }

        public string Codigo { get; set; } = string.Empty;

        public string Valor { get; set; } = string.Empty;

        public string? Grupo { get; set; }

        public string? Descripcion { get; set; }

        public TipoParametro Tipo { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public DateTime? FechaModificacion { get; set; }

        public bool Activo { get; set; } = true;
    }
}