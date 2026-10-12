using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SistemaGestionFerreteria.Application.Features.Facturacion.Models;
using SistemaGestionFerreteria.Application.Features.FacturacionElectronica.Models;
using SistemaGestionFerreteria.Application.Interfaces;
using SistemaGestionFerreteria.Domain.Entities;
using SistemaGestionFerreteria.Domain.Enums;
using SistemaGestionFerreteria.Infrastructure.Persistence;
using SistemaGestionFerreteria.Infrastructure.Services.FacturacionElectronica;

namespace SistemaGestionFerreteria.Infrastructure.Services.Facturacion;

public class FacturaService : IFacturaService
{
    private const string LockName = "MIL-DAN:Facturacion";
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IAfipWsfeService _wsfe;
    private readonly IAfipConfiguracionService _configuration;
    private readonly AfipOptions _options;
    public FacturaService(IDbContextFactory<AppDbContext> factory, IAfipWsfeService wsfe,
        IAfipConfiguracionService configuration, IOptions<AfipOptions> options)
    { _factory = factory; _wsfe = wsfe; _configuration = configuration; _options = options.Value; }

    public async Task<int> ObtenerProximoNumeroAsync(TipoComprobante tipoComprobante, int puntoVenta)
    {
        if (tipoComprobante is TipoComprobante.FacturaA or TipoComprobante.FacturaB)
        {
            var result = await _wsfe.ObtenerUltimoComprobanteAutorizadoAsync(_options.PuntoVenta, tipoComprobante == TipoComprobante.FacturaA ? 1 : 6);
            if (!result.Exitoso) throw new InvalidOperationException(result.Error);
            return result.ProximoNumero;
        }
        await using var db = await _factory.CreateDbContextAsync();
        return (await db.Facturas.Where(x => x.TipoComprobante == tipoComprobante && x.PuntoVenta == puntoVenta && x.AmbienteAfip == null)
            .MaxAsync(x => (int?)x.NumeroComprobante) ?? 0) + 1;
    }

    public async Task<int> CrearAsync(FacturaViewModel model)
    {
        await using var db = await _factory.CreateDbContextAsync();
        await BloquearAsync(db);
        try
        {
            // La misma operación nunca crea una segunda venta, incluso después de un reinicio.
            var existing = await db.Facturas.SingleOrDefaultAsync(x => x.OperacionId == model.OperacionId);
            if (existing is not null) return existing.IdFactura;
            if (model.OperacionId == Guid.Empty) throw new InvalidOperationException("Falta el identificador de operación.");
            if (!model.EnviarAfip && model.TipoComprobante is TipoComprobante.FacturaA or TipoComprobante.FacturaB)
                model.TipoComprobante = TipoComprobante.FacturaX;
            var fiscal = model.TipoComprobante is TipoComprobante.FacturaA or TipoComprobante.FacturaB or TipoComprobante.NotaCredito;
            if (model.TipoComprobante == TipoComprobante.FacturaC) throw new InvalidOperationException("La empresa es responsable inscripto: no puede emitir Factura C.");
            if (fiscal && !model.EnviarAfip) throw new InvalidOperationException("Las notas de crédito fiscales deben autorizarse en ARCA.");
            if (model.Detalles.Count == 0 || model.Detalles.Any(x => string.IsNullOrWhiteSpace(x.Descripcion)))
                throw new InvalidOperationException("Debe cargar detalles con descripción.");
            var client = await db.Clientes.SingleOrDefaultAsync(x => x.IdCliente == model.IdCliente && x.Activo)
                ?? throw new InvalidOperationException("Debe seleccionar un cliente activo.");
            FacturaCalculos.Recalcular(model);
            if (model.Total <= 0) throw new InvalidOperationException("El total debe ser mayor a cero.");
            if (model.Pagos.Any(x => x.Monto < 0) || FacturaCalculos.Redondear(model.Pagos.Sum(x => x.Monto)) != model.Total)
                throw new InvalidOperationException("Los pagos deben sumar el total del comprobante.");
            if (model.MontoRecibido.HasValue && model.Pagos.All(x => x.FormaPago == FormaPago.Efectivo) && model.MontoRecibido < model.Total)
                throw new InvalidOperationException("El efectivo recibido no alcanza para pagar la venta.");
            if (fiscal)
            {
                var state = _configuration.ValidarConfiguracion();
                if (!state.ConfiguracionValida) throw new InvalidOperationException(string.Join(" ", state.Errores));
            }
            var ambiente = Ambiente();
            var cuit = _options.Cuit.Replace("-", "").Trim();
            Factura? associated = null;
            if (model.TipoComprobante == TipoComprobante.NotaCredito)
            {
                associated = await db.Facturas.SingleOrDefaultAsync(x => x.IdFactura == model.IdFacturaAsociada);
                if (associated is null || !associated.EnviadaAfip || associated.Cae == null ||
                    associated.CodigoComprobanteAfip is not (1 or 6) || associated.IdCliente != client.IdCliente ||
                    associated.AmbienteAfip != ambiente || associated.CuitEmisorAfip != cuit)
                    throw new InvalidOperationException("Seleccione una factura A/B autorizada del mismo cliente y ambiente.");
                var credited = await db.Facturas.Where(x => x.IdFacturaAsociada == associated.IdFactura &&
                    x.Estado != EstadoFactura.Anulada && (x.EnviadaAfip || x.ResultadoAfip == "PENDIENTE"))
                    .SumAsync(x => (decimal?)x.Total) ?? 0;
                if (credited + model.Total > associated.Total)
                    throw new InvalidOperationException("La nota de crédito supera el saldo de la factura asociada.");
            }
            var code = model.TipoComprobante switch
            {
                TipoComprobante.FacturaA => 1, TipoComprobante.FacturaB => 6,
                TipoComprobante.NotaCredito => associated!.CodigoComprobanteAfip == 1 ? 3 : 8,
                _ => (int?)null
            };
            if (fiscal && await db.Facturas.AnyAsync(x => x.AmbienteAfip == ambiente && x.CuitEmisorAfip == cuit &&
                x.PuntoVenta == _options.PuntoVenta && x.CodigoComprobanteAfip == code && x.ResultadoAfip == "PENDIENTE"))
                throw new InvalidOperationException("Hay un envío pendiente en esta serie. Recupérelo desde el listado antes de emitir otro.");
            var number = 0;
            if (fiscal)
            {
                var last = await _wsfe.ObtenerUltimoComprobanteAutorizadoAsync(_options.PuntoVenta, code!.Value);
                if (!last.Exitoso) throw new InvalidOperationException(last.Error);
                number = last.ProximoNumero;
            }
            else number = (await db.Facturas.Where(x => x.TipoComprobante == model.TipoComprobante &&
                x.PuntoVenta == _options.PuntoVenta && x.AmbienteAfip == null).MaxAsync(x => (int?)x.NumeroComprobante) ?? 0) + 1;
            var invoice = new Factura
            {
                OperacionId = model.OperacionId, IdCliente = client.IdCliente, TipoComprobante = model.TipoComprobante,
                PuntoVenta = _options.PuntoVenta, NumeroComprobante = number, Fecha = model.Fecha.Date,
                Estado = fiscal ? EstadoFactura.Borrador : EstadoFactura.Emitida,
                CondicionVenta = model.CondicionVenta, Observacion = model.Observacion,
                Subtotal = model.Subtotal, TotalIva21 = model.TotalIva21, TotalIva105 = model.TotalIva105,
                TotalIva27 = model.TotalIva27, TotalExento = model.TotalExento, Total = model.Total,
                MontoRecibido = model.MontoRecibido.HasValue ? FacturaCalculos.Redondear(model.MontoRecibido.Value) : null,
                AmbienteAfip = fiscal ? ambiente : null, CuitEmisorAfip = fiscal ? cuit : null,
                CodigoComprobanteAfip = code, IdFacturaAsociada = associated?.IdFactura,
                IdCajaVenta = fiscal || model.TipoComprobante == TipoComprobante.FacturaX ? model.IdCajaVenta : null,
                ResultadoAfip = fiscal ? "PENDIENTE" : null,
                Detalles = model.Detalles.Select(x => new FacturaDetalle { IdProducto = x.IdProducto, CodigoProducto = x.CodigoProducto,
                    Descripcion = x.Descripcion, Cantidad = x.Cantidad, PrecioUnitario = x.PrecioUnitario,
                    PorcentajeIva = x.PorcentajeIva, ImporteIva = x.ImporteIva, Subtotal = x.Subtotal }).ToList(),
                Pagos = model.Pagos.Select(x => new FacturaPago { FormaPago = x.FormaPago, Monto = x.Monto, Referencia = x.Referencia }).ToList()
            };
            if (fiscal)
            {
                await ValidarCajaAsync(db, invoice);
                invoice.SolicitudAfip = AfipSolicitudFactory.Crear(invoice, client, associated, _options);
            }
            else if (invoice.TipoComprobante == TipoComprobante.FacturaX) await ValidarCajaAsync(db, invoice);
            db.Facturas.Add(invoice);
            // Guardado duradero ANTES de la llamada remota. No se mantiene una transacción SQL durante HTTP.
            if (fiscal)
            {
                await db.SaveChangesAsync();
                await EnviarYGuardarAsync(db, invoice);
            }
            else
            {
                using var transaction = await db.Database.BeginTransactionAsync();
                await db.SaveChangesAsync();
                if (invoice.TipoComprobante == TipoComprobante.FacturaX && Ambiente() == "Produccion")
                    await RegistrarCajaAsync(db, invoice);
                await db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            return invoice.IdFactura;
        }
        finally { await LiberarAsync(db); }
    }

    public async Task ReintentarAfipAsync(int idFactura)
    {
        await using var db = await _factory.CreateDbContextAsync();
        await BloquearAsync(db);
        try
        {
            var invoice = await db.Facturas.Include(x => x.Pagos).Include(x => x.Detalles).Include(x => x.Cliente)
                .SingleOrDefaultAsync(x => x.IdFactura == idFactura) ?? throw new InvalidOperationException("No se encontró el comprobante.");
            if (invoice.EnviadaAfip) return;
            if (invoice.Estado == EstadoFactura.Anulada || invoice.SolicitudAfip is null) throw new InvalidOperationException("Este comprobante no tiene un envío recuperable.");
            if (invoice.AmbienteAfip != Ambiente() || invoice.CuitEmisorAfip != _options.Cuit.Replace("-", "").Trim())
                throw new InvalidOperationException("No se puede recuperar un envío de otro CUIT o ambiente.");
            if (invoice.ResultadoAfip == "PENDIENTE")
            {
                var lookup = await _wsfe.ConsultarAsync(invoice.SolicitudAfip);
                if (lookup.Autorizado) { await GuardarResultadoAsync(db, invoice, lookup); return; }
                if (!lookup.NoEncontrado)
                {
                    invoice.ErrorAfip = lookup.Error;
                    await db.SaveChangesAsync();
                    return;
                }
                // Si no existe, se reenvía exactamente la solicitud guardada; no se cambia fecha ni receptor.
                var last = await _wsfe.ObtenerUltimoComprobanteAutorizadoAsync(invoice.PuntoVenta, invoice.CodigoComprobanteAfip!.Value);
                if (!last.Exitoso || last.ProximoNumero != invoice.NumeroComprobante)
                    throw new InvalidOperationException("No se puede reenviar: la numeración de ARCA cambió. " + last.Error);
            }
            else
            {
                if (await db.Facturas.AnyAsync(x => x.IdFactura != idFactura && x.AmbienteAfip == invoice.AmbienteAfip &&
                    x.CuitEmisorAfip == invoice.CuitEmisorAfip && x.PuntoVenta == invoice.PuntoVenta &&
                    x.CodigoComprobanteAfip == invoice.CodigoComprobanteAfip && x.ResultadoAfip == "PENDIENTE"))
                    throw new InvalidOperationException("Hay otro envío pendiente en esta serie.");
                // Un rechazo explícito permite reconstruir con los datos corregidos del cliente y nuevo número.
                var last = await _wsfe.ObtenerUltimoComprobanteAutorizadoAsync(invoice.PuntoVenta, invoice.CodigoComprobanteAfip!.Value);
                if (!last.Exitoso) throw new InvalidOperationException(last.Error);
                invoice.NumeroComprobante = last.ProximoNumero;
                invoice.Fecha = AfipSolicitudFactory.FechaArgentina();
                var associated = invoice.IdFacturaAsociada.HasValue ? await db.Facturas.FindAsync(invoice.IdFacturaAsociada.Value) : null;
                if (associated is not null)
                {
                    var credits = await db.Facturas.Where(x => x.IdFacturaAsociada == associated.IdFactura && x.IdFactura != idFactura &&
                        x.Estado != EstadoFactura.Anulada && (x.EnviadaAfip || x.ResultadoAfip == "PENDIENTE")).SumAsync(x => (decimal?)x.Total) ?? 0;
                    if (credits + invoice.Total > associated.Total) throw new InvalidOperationException("La nota de crédito supera el saldo de la factura.");
                }
                invoice.SolicitudAfip = AfipSolicitudFactory.Crear(invoice, invoice.Cliente, associated, _options);
                await ValidarCajaAsync(db, invoice);
            }
            invoice.ResultadoAfip = "PENDIENTE";
            invoice.ErrorAfip = null;
            await db.SaveChangesAsync();
            await EnviarYGuardarAsync(db, invoice);
        }
        finally { await LiberarAsync(db); }
    }

    private async Task EnviarYGuardarAsync(AppDbContext db, Factura invoice)
    {
        invoice.FechaEnvioAfip = DateTime.Now;
        await db.SaveChangesAsync();
        var result = await _wsfe.AutorizarAsync(invoice.SolicitudAfip!);
        if (!result.Autorizado && !result.Rechazado)
        {
            var lookup = await _wsfe.ConsultarAsync(invoice.SolicitudAfip!);
            if (lookup.Autorizado) result = lookup;
        }
        await GuardarResultadoAsync(db, invoice, result);
    }

    private async Task GuardarResultadoAsync(AppDbContext db, Factura invoice, AfipAutorizacionResultado result)
    {
        using var transaction = await db.Database.BeginTransactionAsync();
        invoice.EnviadaAfip = result.Autorizado;
        invoice.Cae = result.Cae;
        invoice.FechaVencimientoCae = result.Vencimiento;
        invoice.Estado = result.Autorizado ? EstadoFactura.Emitida : EstadoFactura.Borrador;
        invoice.ResultadoAfip = result.Autorizado ? "A" : result.Rechazado ? "R" : "PENDIENTE";
        invoice.ErrorAfip = result.Autorizado ? result.Observaciones : result.Error;
        invoice.FechaModificacion = DateTime.Now;
        // Homologación nunca suma a la caja real. En producción el movimiento y el CAE se guardan juntos.
        if (result.Autorizado && invoice.AmbienteAfip == "Produccion")
            await RegistrarCajaAsync(db, invoice);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static async Task RegistrarCajaAsync(AppDbContext db, Factura invoice)
    {
            var cash = invoice.Pagos.Where(x => x.FormaPago == FormaPago.Efectivo).Sum(x => x.Monto);
            if (cash > 0 && invoice.IdCajaVenta.HasValue && !await db.CajaMovimientos.AnyAsync(x => x.IdFactura == invoice.IdFactura &&
                (x.Tipo == TipoMovimientoCaja.Venta || x.Tipo == TipoMovimientoCaja.AnulacionVenta)))
            {
                var caja = await db.Cajas.FindAsync(invoice.IdCajaVenta.Value) ?? throw new InvalidOperationException("No existe la caja del comprobante.");
                var credit = invoice.TipoComprobante == TipoComprobante.NotaCredito;
                caja.TotalVentas += credit ? -cash : cash;
                caja.TotalFinal = caja.FondoInicial + caja.TotalIngresos + caja.TotalVentas - caja.TotalRetiros;
                caja.FechaModificacion = DateTime.Now;
                db.CajaMovimientos.Add(new CajaMovimiento { IdCaja = caja.IdCaja, IdFactura = invoice.IdFactura,
                    Tipo = credit ? TipoMovimientoCaja.AnulacionVenta : TipoMovimientoCaja.Venta, Monto = cash,
                    Descripcion = (credit ? "Nota de crédito " : "Factura ") + invoice.PuntoVenta.ToString("D5") + "-" + invoice.NumeroComprobante.ToString("D8") });
            }
    }

    private async Task ValidarCajaAsync(AppDbContext db, Factura invoice)
    {
        if ((invoice.AmbienteAfip != "Produccion" && !(invoice.TipoComprobante == TipoComprobante.FacturaX && Ambiente() == "Produccion")) ||
            !invoice.Pagos.Any(x => x.FormaPago == FormaPago.Efectivo && x.Monto > 0)) return;
        if (!invoice.IdCajaVenta.HasValue || !await db.Cajas.AnyAsync(x => x.IdCaja == invoice.IdCajaVenta && x.Activo && x.Estado == EstadoCaja.Abierta))
            throw new InvalidOperationException("Debe seleccionar una caja abierta para el pago en efectivo.");
    }

    public async Task<List<FacturaListadoViewModel>> ObtenerTodasAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Facturas.AsNoTracking().OrderByDescending(x => x.FechaAlta).Select(x => new FacturaListadoViewModel
        {
            IdFactura = x.IdFactura, Fecha = x.Fecha, TipoComprobante = x.TipoComprobante, PuntoVenta = x.PuntoVenta,
            NumeroComprobante = x.NumeroComprobante, Cliente = x.Cliente.Nombre + " " + (x.Cliente.Apellido ?? ""),
            Total = x.Total, Estado = x.Estado, EnviadaAfip = x.EnviadaAfip, Cae = x.Cae, ResultadoAfip = x.ResultadoAfip,
            ErrorAfip = x.ErrorAfip, AmbienteAfip = x.AmbienteAfip
        }).ToListAsync();
    }

    public async Task<FacturaViewModel?> ObtenerPorIdAsync(int idFactura)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Facturas.AsNoTracking().Where(x => x.IdFactura == idFactura).Select(x => new FacturaViewModel
        {
            IdFactura = x.IdFactura, OperacionId = x.OperacionId ?? Guid.Empty, IdCliente = x.IdCliente,
            TipoComprobante = x.TipoComprobante, PuntoVenta = x.PuntoVenta, NumeroComprobante = x.NumeroComprobante,
            Fecha = x.Fecha, Estado = x.Estado, CondicionVenta = x.CondicionVenta, Observacion = x.Observacion,
            EnviarAfip = x.AmbienteAfip != null, EnviadaAfip = x.EnviadaAfip, Cae = x.Cae, FechaVencimientoCae = x.FechaVencimientoCae,
            ResultadoAfip = x.ResultadoAfip, ErrorAfip = x.ErrorAfip, AmbienteAfip = x.AmbienteAfip,
            IdCajaVenta = x.IdCajaVenta, IdFacturaAsociada = x.IdFacturaAsociada,
            Subtotal = x.Subtotal, TotalIva21 = x.TotalIva21, TotalIva105 = x.TotalIva105, TotalIva27 = x.TotalIva27,
            TotalExento = x.TotalExento, Total = x.Total, MontoRecibido = x.MontoRecibido,
            Detalles = x.Detalles.Select(d => new FacturaDetalleViewModel { IdProducto = d.IdProducto, CodigoProducto = d.CodigoProducto,
                Descripcion = d.Descripcion, Cantidad = d.Cantidad, PrecioUnitario = d.PrecioUnitario, PorcentajeIva = d.PorcentajeIva,
                ImporteIva = d.ImporteIva, Subtotal = d.Subtotal }).ToList(),
            Pagos = x.Pagos.Select(p => new FacturaPagoViewModel { FormaPago = p.FormaPago, Monto = p.Monto, Referencia = p.Referencia }).ToList()
        }).FirstOrDefaultAsync();
    }

    public async Task AnularAsync(int idFactura)
    {
        await using var db = await _factory.CreateDbContextAsync();
        await BloquearAsync(db);
        try
        {
            var invoice = await db.Facturas.FindAsync(idFactura);
            if (invoice is null || invoice.Estado == EstadoFactura.Anulada) return;
            if (invoice.EnviadaAfip || invoice.Cae is not null) throw new InvalidOperationException("Una factura autorizada se revierte con una nota de crédito, no con anulación local.");
            if (invoice.ResultadoAfip == "PENDIENTE") throw new InvalidOperationException("Primero debe resolver el envío pendiente para confirmar si ARCA lo autorizó.");
            using var transaction = await db.Database.BeginTransactionAsync();
            // Comprobantes locales antiguos pueden tener un ingreso de caja registrado.
            var cash = await db.CajaMovimientos.Where(x => x.IdFactura == idFactura && x.Activo && x.Tipo == TipoMovimientoCaja.Venta)
                .SumAsync(x => (decimal?)x.Monto) ?? 0;
            if (cash > 0 && !await db.CajaMovimientos.AnyAsync(x => x.IdFactura == idFactura && x.Tipo == TipoMovimientoCaja.AnulacionVenta))
            {
                var caja = await db.Cajas.SingleOrDefaultAsync(x => x.Activo && x.Estado == EstadoCaja.Abierta)
                    ?? throw new InvalidOperationException("Debe abrir caja para revertir el ingreso de este comprobante local.");
                caja.TotalVentas -= cash;
                caja.TotalFinal = caja.FondoInicial + caja.TotalIngresos + caja.TotalVentas - caja.TotalRetiros;
                db.CajaMovimientos.Add(new CajaMovimiento { IdCaja = caja.IdCaja, IdFactura = idFactura,
                    Tipo = TipoMovimientoCaja.AnulacionVenta, Monto = cash, Descripcion = "Anulación local #" + idFactura });
            }
            invoice.Estado = EstadoFactura.Anulada;
            invoice.FechaModificacion = DateTime.Now;
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        finally { await LiberarAsync(db); }
    }

    private string Ambiente() => string.Equals(_options.Ambiente, "Produccion", StringComparison.OrdinalIgnoreCase) ? "Produccion" : "Homologacion";
    private static async Task BloquearAsync(AppDbContext db)
    {
        await db.Database.OpenConnectionAsync();
        using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "DECLARE @r int; EXEC @r = sys.sp_getapplock @Resource=@resource, @LockMode='Exclusive', @LockOwner='Session', @LockTimeout=15000; SELECT @r;";
        var p = command.CreateParameter(); p.ParameterName = "@resource"; p.Value = LockName; command.Parameters.Add(p);
        if (Convert.ToInt32(await command.ExecuteScalarAsync()) < 0) throw new InvalidOperationException("Hay otra emisión en curso. Espere y vuelva a intentar.");
    }
    private static async Task LiberarAsync(AppDbContext db)
    {
        using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "EXEC sys.sp_releaseapplock @Resource=@resource, @LockOwner='Session';";
        var p = command.CreateParameter(); p.ParameterName = "@resource"; p.Value = LockName; command.Parameters.Add(p);
        await command.ExecuteNonQueryAsync();
        await db.Database.CloseConnectionAsync();
    }
}
