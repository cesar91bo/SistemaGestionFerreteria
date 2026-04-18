using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;

namespace SistemaGestionFerreteria.Application.Features.Clientes.Models
{
    public class ClienteViewModel : IValidatableObject
    {
        public int IdCliente { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        public string Nombre { get; set; } = string.Empty;

        public string? Apellido { get; set; }
        public string? Documento { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public string? Direccion { get; set; }
        public bool RequiereFactura { get; set; }
        public string? Cuit { get; set; }
        public Domain.Enums.TipoRegimenImpositivo RegimenImpositivo { get; set; }
        public bool Activo { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Si requiere factura, CUIT obligatorio
            if (RequiereFactura && string.IsNullOrWhiteSpace(Cuit))
            {
                yield return new ValidationResult("El CUIT es obligatorio cuando el cliente requiere factura.", new[] { nameof(Cuit) });
            }

            // Regímenes que requieren CUIT: ResponsableInscripto y Monotributista
            if ((RegimenImpositivo == Domain.Enums.TipoRegimenImpositivo.ResponsableInscripto ||
                 RegimenImpositivo == Domain.Enums.TipoRegimenImpositivo.Monotributista) &&
                string.IsNullOrWhiteSpace(Cuit))
            {
                yield return new ValidationResult($"Para el régimen '{RegimenImpositivo}' el CUIT es obligatorio.", new[] { nameof(Cuit) });
            }

            // Validación completa de CUIT (dígito verificador)
            if (!string.IsNullOrWhiteSpace(Cuit))
            {
                var digits = new string(Cuit.Where(char.IsDigit).ToArray());
                if (digits.Length != 11)
                {
                    yield return new ValidationResult("El CUIT debe tener 11 dígitos numéricos.", new[] { nameof(Cuit) });
                }
                else if (!EsCuitValido(digits))
                {
                    yield return new ValidationResult("CUIT inválido (dígito verificador incorrecto).", new[] { nameof(Cuit) });
                }
            }
        }

        private static bool EsCuitValido(string cuitDigits)
        {
            // cuitDigits: solo dígitos, longitud 11
            if (string.IsNullOrWhiteSpace(cuitDigits) || cuitDigits.Length != 11 || !cuitDigits.All(char.IsDigit))
                return false;

            var weights = new int[] { 5, 4, 3, 2, 7, 6, 5, 4, 3, 2 };

            int sum = 0;
            for (int i = 0; i < 10; i++)
            {
                sum += (cuitDigits[i] - '0') * weights[i];
            }

            int mod = sum % 11;
            int check = 11 - mod;

            if (check == 11) check = 0;
            else if (check == 10) check = 9;

            int lastDigit = cuitDigits[10] - '0';

            return check == lastDigit;
        }
    }
}
