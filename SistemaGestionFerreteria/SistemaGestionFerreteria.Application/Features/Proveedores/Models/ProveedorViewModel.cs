namespace SistemaGestionFerreteria.Application.Features.Proveedores.Models
{
    public class ProveedorViewModel
    {
        public int IdProveedor { get; set; }

        public string Descripcion { get; set; } = string.Empty;

        public string? Telefono { get; set; }

        public string? Email { get; set; }

        public string? Direccion { get; set; }

        public bool Activo { get; set; } = true;
    }
}