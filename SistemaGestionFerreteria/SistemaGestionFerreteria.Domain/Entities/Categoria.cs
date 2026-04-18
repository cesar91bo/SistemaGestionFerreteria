namespace SistemaGestionFerreteria.Domain.Entities
{
    public class Categoria
    {
        public int IdCategoria { get; set; }

        public string Descripcion { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;

        public ICollection<Producto> Productos { get; set; } = new List<Producto>();
    }
}