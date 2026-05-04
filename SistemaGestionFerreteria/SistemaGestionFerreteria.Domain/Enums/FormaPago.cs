// Domain/Enums/FormaPago.cs
using System.ComponentModel.DataAnnotations;

namespace SistemaGestionFerreteria.Domain.Enums
{
    public enum FormaPago
    {
        [Display(Name = "Efectivo")]
        Efectivo = 0,

        [Display(Name = "Transferencia")]
        Transferencia = 1,

        [Display(Name = "Tarjeta Débito")]
        TarjetaDebito = 2,

        [Display(Name = "Tarjeta Crédito")]
        TarjetaCredito = 3,

        [Display(Name = "Cuenta Corriente")]
        CuentaCorriente = 4,

        [Display(Name = "Mercado Pago")]
        MercadoPago = 5,

        [Display(Name = "Otro")]
        Otro = 99
    }
}