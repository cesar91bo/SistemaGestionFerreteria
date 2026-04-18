namespace SistemaGestionFerreteria.Domain.Entities
{
    public class Proveedor
    {
        public int IdProveedor { get; set; }

        public string Descripcion { get; set; } = string.Empty;

        public string? Telefono { get; set; }

        public string? Email { get; set; }

        public string? Direccion { get; set; }

        public bool Activo { get; set; } = true;

        public ICollection<Producto> Productos { get; set; } = new List<Producto>();
    }
}