// Domain/Enums/FormaPago.cs
namespace SistemaGestionFerreteria.Domain.Enums
{
    public enum FormaPago
    {
        Efectivo = 0,
        Transferencia = 1,
        TarjetaDebito = 2,
        TarjetaCredito = 3,
        CuentaCorriente = 4,
        MercadoPago = 5,
        Otro = 99
    }
}