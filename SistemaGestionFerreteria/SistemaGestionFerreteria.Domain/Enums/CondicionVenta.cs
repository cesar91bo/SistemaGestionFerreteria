using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SistemaGestionFerreteria.Domain.Enums
{
    public enum CondicionVenta
    {
        Contado = 0,
        CuentaCorriente = 1,
        Transferencia = 2,
        Tarjeta = 3
    }
}
