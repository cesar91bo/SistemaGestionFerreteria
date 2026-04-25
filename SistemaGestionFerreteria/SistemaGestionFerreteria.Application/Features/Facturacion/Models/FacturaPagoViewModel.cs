// Application/Features/Facturacion/Models/FacturaPagoViewModel.cs
using SistemaGestionFerreteria.Domain.Enums;

namespace SistemaGestionFerreteria.Application.Features.Facturacion.Models
{
    public class FacturaPagoViewModel
    {
        public FormaPago FormaPago { get; set; }

        public decimal Monto { get; set; }

        public string? Referencia { get; set; }
    }
}