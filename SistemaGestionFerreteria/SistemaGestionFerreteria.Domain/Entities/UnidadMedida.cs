namespace SistemaGestionFerreteria.Domain.Entities
{
    public class UnidadMedida
    {
        public int IdUnidadMedida { get; set; }

        public string Descripcion { get; set; } = string.Empty;

        // Opcional: abreviatura visible
        public string? Abreviatura { get; set; } // kg, lt, m, un, cm, etc.

        public bool Activo { get; set; } = true;

        public ICollection<Producto> Productos { get; set; } = new List<Producto>();
    }
}