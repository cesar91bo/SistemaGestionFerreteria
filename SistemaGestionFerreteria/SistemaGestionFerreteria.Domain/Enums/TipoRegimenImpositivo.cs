using System.ComponentModel.DataAnnotations;

namespace SistemaGestionFerreteria.Domain.Enums
{
    public enum TipoRegimenImpositivo
    {
        [Display(Name = "Consumidor Final")]
        ConsumidorFinal = 0,

        [Display(Name = "Monotributista")]
        Monotributista = 1,

        [Display(Name = "Responsable Inscripto")]
        ResponsableInscripto = 2,

        [Display(Name = "Exento")]
        Exento = 3,

        [Display(Name = "No Responsable")]
        NoResponsable = 4,

        [Display(Name = "Otros")]
        Otros = 99
    }
}