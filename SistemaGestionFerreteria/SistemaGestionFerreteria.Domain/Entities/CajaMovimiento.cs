using SistemaGestionFerreteria.Domain.Enums;

namespace SistemaGestionFerreteria.Domain.Entities
{
    public class CajaMovimiento
    {
        public int IdCajaMovimiento { get; set; }

        public int IdCaja { get; set; }

        public Caja Caja { get; set; } = null!;

        public int? IdFactura { get; set; }

        public Factura? Factura { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        public TipoMovimientoCaja Tipo { get; set; }

        public decimal Monto { get; set; }

        public string Descripcion { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;
    }
}