using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SistemaGestionFerreteria.Domain.Enums
{
    public enum TipoActualizacionPrecio
    {
        AumentarPorcentaje = 1,
        DisminuirPorcentaje = 2,
        ReemplazarMargen = 3,
        MargenPorDefecto = 4
    }
}
