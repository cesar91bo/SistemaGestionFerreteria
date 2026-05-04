using System.ComponentModel.DataAnnotations;

public enum CondicionVenta
{
    [Display(Name = "Contado")]
    Contado = 0,

    [Display(Name = "Cuenta Corriente")]
    CuentaCorriente = 1,

    [Display(Name = "Transferencia")]
    Transferencia = 2,

    [Display(Name = "Tarjeta")]
    Tarjeta = 3
}