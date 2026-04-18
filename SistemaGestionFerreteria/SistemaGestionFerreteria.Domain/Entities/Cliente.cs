namespace SistemaGestionFerreteria.Domain.Entities
{
    public class Cliente
    {
        public int IdCliente { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string? Apellido { get; set; }

        public string? Documento { get; set; }

        public string? Telefono { get; set; }

        public string? Email { get; set; }

        public string? Direccion { get; set; }

        public bool RequiereFactura { get; set; } = false;

        public string? Cuit { get; set; }

        public Enums.TipoRegimenImpositivo RegimenImpositivo { get; set; } = Enums.TipoRegimenImpositivo.ConsumidorFinal;

        public bool Activo { get; set; } = true;

        public DateTime FechaAlta { get; set; }
    }
}