using Microsoft.Extensions.Options;
using SistemaGestionFerreteria.Application.Features.FacturacionElectronica.Models;
using SistemaGestionFerreteria.Application.Interfaces;

namespace SistemaGestionFerreteria.Infrastructure.Services.FacturacionElectronica
{
    public class AfipConfiguracionService : IAfipConfiguracionService
    {
        private readonly AfipOptions _options;

        public AfipConfiguracionService(IOptions<AfipOptions> options)
        {
            _options = options.Value;
        }

        public AfipEstadoConfiguracionViewModel ValidarConfiguracion()
        {
            var resultado = new AfipEstadoConfiguracionViewModel
            {
                Ambiente = _options.Ambiente,
                Cuit = _options.Cuit,
                PuntoVenta = _options.PuntoVenta,
                Servicio = _options.Servicio,
                WsaaUrl = _options.WsaaUrl,
                WsfeUrl = _options.WsfeUrl,
                ModoCertificado = _options.Certificado.Modo,
                RutaPfx = _options.Certificado.RutaPfx
            };

            if (string.IsNullOrWhiteSpace(_options.Ambiente))
                resultado.Errores.Add("No está configurado el ambiente de AFIP.");

            if (string.IsNullOrWhiteSpace(_options.Cuit))
                resultado.Errores.Add("No está configurado el CUIT.");

            if (_options.PuntoVenta <= 0)
                resultado.Errores.Add("El punto de venta debe ser mayor a cero.");

            if (string.IsNullOrWhiteSpace(_options.Servicio))
                resultado.Errores.Add("No está configurado el servicio AFIP.");

            if (string.IsNullOrWhiteSpace(_options.WsaaUrl))
                resultado.Errores.Add("No está configurada la URL de WSAA.");

            if (string.IsNullOrWhiteSpace(_options.WsfeUrl))
                resultado.Errores.Add("No está configurada la URL de WSFE.");

            if (string.Equals(_options.Certificado.Modo, "Pfx", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(_options.Certificado.RutaPfx))
                {
                    resultado.Errores.Add("El modo de certificado es Pfx, pero no se configuró la ruta del archivo.");
                }
                else
                {
                    resultado.ExisteCertificado = File.Exists(_options.Certificado.RutaPfx);

                    if (!resultado.ExisteCertificado)
                    {
                        resultado.Errores.Add("No se encontró el archivo PFX en la ruta configurada.");
                    }
                }
            }
            else if (string.Equals(_options.Certificado.Modo, "Store", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(_options.Certificado.Serial))
                {
                    resultado.Errores.Add("El modo de certificado es Store, pero no se configuró el serial.");
                }
            }
            else
            {
                resultado.Errores.Add("El modo de certificado debe ser Pfx o Store.");
            }

            resultado.ConfiguracionValida = resultado.Errores.Count == 0;

            return resultado;
        }
    }
}