using System.ComponentModel.DataAnnotations;

namespace SistemaGestionFerreteria.Application.Features.Categorias.Models
{
    public class CategoriaViewModel
    {
        public int IdCategoria { get; set; }

        [Required(ErrorMessage = "Debe ingresar la descripción.")]
        public string Descripcion { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;
    }
}