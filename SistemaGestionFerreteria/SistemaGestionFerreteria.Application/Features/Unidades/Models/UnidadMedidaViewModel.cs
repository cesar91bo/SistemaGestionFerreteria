using System.ComponentModel.DataAnnotations;

namespace SistemaGestionFerreteria.Application.Features.Unidades.Models
{
    public class UnidadMedidaViewModel
    {
        public int IdUnidadMedida { get; set; }

        [Required(ErrorMessage = "La descripción es obligatoria.")]
        [StringLength(100, ErrorMessage = "La descripción no puede superar los 100 caracteres.")]
        public string Descripcion { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "La abreviatura no puede superar los 20 caracteres.")]
        public string? Abreviatura { get; set; }

        public bool Activo { get; set; }
    }
}