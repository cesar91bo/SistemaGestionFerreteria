namespace SistemaGestionFerreteria.Application.Features.FacturacionElectronica.Models;

public class AfipAutorizacionResultado
{
    public bool Autorizado { get; set; }
    public bool Rechazado { get; set; }
    public bool NoEncontrado { get; set; }
    public string? Cae { get; set; }
    public DateTime? Vencimiento { get; set; }
    public string? Error { get; set; }
    public string? Observaciones { get; set; }
}
