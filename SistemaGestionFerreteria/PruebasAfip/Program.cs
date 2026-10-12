using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using System.Xml.Schema;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SistemaGestionFerreteria.Application.Features.Facturacion.Models;
using SistemaGestionFerreteria.Application.Features.FacturacionElectronica.Models;
using SistemaGestionFerreteria.Application.Interfaces;
using SistemaGestionFerreteria.Domain.Entities;
using SistemaGestionFerreteria.Domain.Enums;
using SistemaGestionFerreteria.Infrastructure.Persistence;
using SistemaGestionFerreteria.Infrastructure.Services.Facturacion;
using SistemaGestionFerreteria.Infrastructure.Services.FacturacionElectronica;
using SistemaGestionFerreteria.Infrastructure.Services.Productos;

var tests = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FALLÓ: " + name);
    tests++;
    Console.WriteLine("OK: " + name);
}
async Task Throws(Func<Task> action, string name)
{
    try { await action(); } catch (InvalidOperationException) { Check(true, name); return; }
    Check(false, name);
}
var options = new AfipOptions { Ambiente = "Homologacion", Cuit = "20252443011", PuntoVenta = 8, Servicio = "wsfe",
    WsaaUrl = "https://wsaahomo.afip.gov.ar/ws/services/LoginCms", WsfeUrl = "https://wswhomo.afip.gov.ar/wsfev1/service.asmx" };
FacturaViewModel Model() => new()
{
    Fecha = AfipSolicitudFactory.FechaArgentina(), TipoComprobante = TipoComprobante.FacturaB,
    Detalles = new() {
        new() { Descripcion = "IVA 21", Cantidad = 1, PrecioUnitario = 121, PorcentajeIva = 21 },
        new() { Descripcion = "IVA 10,5", Cantidad = 1, PrecioUnitario = 110.5m, PorcentajeIva = 10.5m },
        new() { Descripcion = "IVA 27", Cantidad = 1, PrecioUnitario = 127, PorcentajeIva = 27 },
        new() { Descripcion = "Exento", Cantidad = 1, PrecioUnitario = 50, PorcentajeIva = 0 } }
};
var model = Model();
FacturaCalculos.Recalcular(model);
Check(model.Total == 408.5m && model.Subtotal == 350 && model.TotalExento == 50 &&
    model.TotalIva21 == 21 && model.TotalIva105 == 10.5m && model.TotalIva27 == 27, "IVA mixto, exentos y total");
var invoice = new Factura { CodigoComprobanteAfip = 6, PuntoVenta = 8, NumeroComprobante = 1, Fecha = model.Fecha,
    Total = model.Total, Subtotal = model.Subtotal, TotalIva21 = model.TotalIva21, TotalIva105 = model.TotalIva105,
    TotalIva27 = model.TotalIva27, TotalExento = model.TotalExento,
    Detalles = model.Detalles.Select(d => new FacturaDetalle { Subtotal = d.Subtotal, PorcentajeIva = d.PorcentajeIva }).ToList() };
var client = new Cliente { Nombre = "Prueba", RegimenImpositivo = TipoRegimenImpositivo.ConsumidorFinal };
var requestXml = AfipSolicitudFactory.Crear(invoice, client, null, options);
XNamespace ns = "http://ar.gov.afip.dif.FEV1/";
var request = XElement.Parse(requestXml);
var wsdlPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../PruebasAfip/wsfe-homologacion.wsdl"));
var schemas = new XmlSchemaSet();
foreach (var schema in XDocument.Load(wsdlPath).Descendants(XName.Get("schema", "http://www.w3.org/2001/XMLSchema")))
{
    using var reader = schema.CreateReader();
    schemas.Add(XmlSchema.Read(reader, null)!);
}
void ValidateRequest(string xml)
{
    var operation = new XDocument(new XElement(ns + "FECAESolicitar",
        new XElement(ns + "Auth", new XElement(ns + "Token", "token"), new XElement(ns + "Sign", "sign"), new XElement(ns + "Cuit", options.Cuit)), XElement.Parse(xml)));
    operation.Validate(schemas, (_, e) => throw new Exception("Esquema WSFE: " + e.Message));
}
ValidateRequest(requestXml);
Check(true, "Solicitud B válida contra XSD oficial de homologación");
Check(request.Descendants(ns + "ImpNeto").Single().Value == "300.00" && request.Descendants(ns + "CondicionIVAReceptorId").Single().Value == "5", "WSFE excluye exentos de ImpNeto e incluye condición IVA");
Check(request.Descendants(ns + "AlicIva").Select(x => x.Element(ns + "Id")!.Value).SequenceEqual(new[] { "4", "5", "6" }), "Códigos oficiales de alícuotas");
invoice.CodigoComprobanteAfip = 1;
await Throws(() => Task.Run(() => AfipSolicitudFactory.Crear(invoice, client, null, options)), "Factura A rechaza consumidor final");
client.RegimenImpositivo = TipoRegimenImpositivo.ResponsableInscripto;
client.Cuit = options.Cuit;
var aXml = AfipSolicitudFactory.Crear(invoice, client, null, options);
ValidateRequest(aXml);
Check(true, "Solicitud A válida contra XSD oficial");
Check(XElement.Parse(aXml).Descendants(ns + "DocTipo").Single().Value == "80", "Factura A con CUIT válido");
invoice.CodigoComprobanteAfip = 6; client.RegimenImpositivo = TipoRegimenImpositivo.ConsumidorFinal; client.Cuit = null;
invoice.Total = 10000000;
await Throws(() => Task.Run(() => AfipSolicitudFactory.Crear(invoice, client, null, options)), "Identificación obligatoria de consumidor final");
invoice.Total = model.Total;

string Envelope(XElement body) => new XElement(XName.Get("Envelope", "http://schemas.xmlsoap.org/soap/envelope/"),
    new XElement(XName.Get("Body", "http://schemas.xmlsoap.org/soap/envelope/"), body)).ToString();
var submitted = request.Descendants(ns + "FECAEDetRequest").Single();
XElement Response(string result, string cae = "12345678901234") => new(ns + "FECAESolicitarResponse",
    new XElement(ns + "FECAESolicitarResult", new XElement(ns + "FeDetResp",
        new XElement(ns + "FECAEDetResponse", submitted.Elements().Where(x => new[] { "Concepto", "DocTipo", "DocNro", "CbteDesde", "CbteHasta", "CbteFch" }.Contains(x.Name.LocalName)),
            new XElement(ns + "Resultado", result), new XElement(ns + "CAE", cae),
            new XElement(ns + "CAEFchVto", DateTime.Today.AddDays(10).ToString("yyyyMMdd")),
            result == "R" ? new XElement(ns + "Observaciones", new XElement(ns + "Obs", new XElement(ns + "Code", 100), new XElement(ns + "Msg", "Rechazo de prueba"))) : null))));
var handler = new FakeHttp(Envelope(Response("A")));
var wsfe = new AfipWsfeService(Options.Create(options), new FakeLogin(), new HttpClient(handler));
var authorized = await wsfe.AutorizarAsync(requestXml);
Check(authorized.Autorizado && authorized.Cae == "12345678901234", "Parseo aprobación y vencimiento CAE");
Check(XElement.Parse(handler.LastBody!).Descendants(ns + "Token").Single().Value == "token<&", "Escape XML de credenciales");
handler.Response = Envelope(Response("R"));
var rejected = await wsfe.AutorizarAsync(requestXml);
Check(rejected.Rechazado && !rejected.Autorizado && rejected.Error!.Contains("Rechazo"), "Rechazo conserva observaciones");
handler.Response = Envelope(Response("A", ""));
Check(!(await wsfe.AutorizarAsync(requestXml)).Autorizado, "Aprobación sin CAE queda indeterminada");
handler.Response = "<html>error</html>";
Check(!(await wsfe.AutorizarAsync(requestXml)).Rechazado, "Respuesta incompleta no implica rechazo seguro");
handler.Status = HttpStatusCode.InternalServerError;
var unavailable = await wsfe.ObtenerUltimoComprobanteAutorizadoAsync(8, 6);
Check(!unavailable.Exitoso && unavailable.Error!.Contains("error interno HTTP 500"), "Error HTML 500 de ARCA muestra diagnóstico claro");
Check(!(await wsfe.AutorizarAsync(requestXml)).Rechazado, "Error interno ARCA no implica rechazo del comprobante");
handler.Status = HttpStatusCode.OK;
handler.Response = Envelope(new XElement(ns + "FEParamGetPtosVentaResponse", new XElement(ns + "ResultGet",
    new XElement(ns + "PtoVenta", new XElement(ns + "Nro", 8), new XElement(ns + "EmisionTipo", "CAE - Ri Iva"), new XElement(ns + "Bloqueado", "N")))));
Check(await wsfe.ValidarPuntoVentaAsync() is null, "Punto de venta CAE de responsable inscripto");
handler.Response = handler.Response.Replace("CAE - Ri Iva", "CAEA");
Check(await wsfe.ValidarPuntoVentaAsync() is not null, "Punto de venta CAEA no se confunde con CAE");
XElement ConsultResult() => new(ns + "FECompConsultarResponse", new XElement(ns + "FECompConsultarResult",
    new XElement(ns + "ResultGet", submitted.Elements(), new XElement(ns + "Resultado", "A"),
        new XElement(ns + "EmisionTipo", "CAE"), new XElement(ns + "CodAutorizacion", "12345678901234"),
        new XElement(ns + "FchVto", DateTime.Today.AddDays(10).ToString("yyyyMMdd")),
        new XElement(ns + "PtoVta", 8), new XElement(ns + "CbteTipo", 6))));
handler.Response = Envelope(ConsultResult());
Check((await wsfe.ConsultarAsync(requestXml)).Autorizado, "Recuperación de CAE por FECompConsultar");
var mismatch = ConsultResult();
mismatch.Descendants(ns + "ImpTotal").Single().Value = "999.00";
handler.Response = Envelope(mismatch);
Check(!(await wsfe.ConsultarAsync(requestXml)).Autorizado, "No recuperar un CAE de otro importe");
handler.Response = Envelope(new XElement("FECompConsultarResponse", new XElement("Errors", new XElement("Err", new XElement("Code", 602), new XElement("Msg", "No existe")))));
Check((await wsfe.ConsultarAsync(requestXml)).NoEncontrado, "Código 602 confirma ausencia");
handler.Response = Envelope(new XElement("FECompConsultarResponse", new XElement("Errors", new XElement("Err", new XElement("Code", 500), new XElement("Msg", "Error")))));
Check(!(await wsfe.ConsultarAsync(requestXml)).NoEncontrado, "Otros errores no habilitan reenvío");

var configPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../SistemaGestionFerreteria/appsettings.json"));
if (args.Contains("--sql"))
{
    using var cfg = JsonDocument.Parse(File.ReadAllText(configPath));
    var connection = cfg.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString()!;
    var builder = new SqlConnectionStringBuilder(connection);
    var name = "MIL_DAN_AFIP_TEST_" + Guid.NewGuid().ToString("N");
    builder.InitialCatalog = "master";
    await using var master = new SqlConnection(builder.ConnectionString);
    await master.OpenAsync();
    await using var create = master.CreateCommand();
    create.CommandText = "CREATE DATABASE [" + name + "]";
    await create.ExecuteNonQueryAsync();
    builder.InitialCatalog = name;
    var dbOptions = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(builder.ConnectionString).Options;
    var factory = new TestFactory(dbOptions);
    try
    {
        await using var setup = new AppDbContext(dbOptions);
        await setup.Database.MigrateAsync();
        var consumer = new Cliente { Nombre = "Cliente prueba", RegimenImpositivo = TipoRegimenImpositivo.ConsumidorFinal, FechaAlta = DateTime.Now };
        var cashBox = new SistemaGestionFerreteria.Domain.Entities.Caja { Estado = EstadoCaja.Abierta, Activo = true, FechaApertura = DateTime.Now };
        setup.Clientes.Add(consumer); setup.Cajas.Add(cashBox); await setup.SaveChangesAsync();
        var category = new Categoria { Descripcion = "Categoría prueba" };
        foreach (var rate in new[] { TipoIvaProducto.Iva105, TipoIvaProducto.Iva21, TipoIvaProducto.Iva27, TipoIvaProducto.Exento })
            setup.Productos.Add(new Producto { Descripcion = "Producto " + rate, Categoria = category,
                Precios = new List<ProductoPrecio> { new() { PrecioVenta = 100, TipoIva = rate, FechaDesde = DateTime.Now } } });
        await setup.SaveChangesAsync();
        var products = await new ProductoService(factory).ObtenerTodosAsync();
        Check(products.Single(x => x.Descripcion == "Producto Iva105").PorcentajeIvaActual == 10.5m &&
            products.Single(x => x.Descripcion == "Producto Iva27").PorcentajeIvaActual == 27m &&
            products.Single(x => x.Descripcion == "Producto Exento").PorcentajeIvaActual == 0m, "SQL: factura toma la alícuota del precio vigente del producto");
        options.Ambiente = "Produccion";
        var remote = new FakeWsfe();
        var service = new FacturaService(factory, remote, new ValidConfiguration(), Options.Create(options));
        FacturaViewModel Sale()
        {
            var m = Model(); FacturaCalculos.Recalcular(m); m.IdCliente = consumer.IdCliente; m.IdCajaVenta = cashBox.IdCaja;
            m.Pagos.Add(new() { FormaPago = FormaPago.Efectivo, Monto = m.Total }); return m;
        }
        var sale = Sale();
        var id = await service.CrearAsync(sale);
        var saved = await service.ObtenerPorIdAsync(id);
        Check(saved!.EnviadaAfip && saved.PuntoVenta == 8 && saved.NumeroComprobante == 1, "SQL: emisión usa numeración remota y punto de venta configurado");
        Check(await service.CrearAsync(sale) == id && remote.Sent == 1, "SQL: doble clic no duplica envío");
        setup.ChangeTracker.Clear();
        Check((await setup.Cajas.FindAsync(cashBox.IdCaja))!.TotalVentas == sale.Total && await setup.CajaMovimientos.CountAsync() == 1, "SQL: CAE y caja se guardan juntos, una sola vez");
        await Throws(() => service.AnularAsync(id), "SQL: no anular localmente factura autorizada");
        remote.Mode = "lost";
        var recoveredId = await service.CrearAsync(Sale());
        Check((await service.ObtenerPorIdAsync(recoveredId))!.EnviadaAfip && remote.Sent == 2, "SQL: respuesta perdida recupera CAE sin reenviar");
        remote.Mode = "pending";
        var pendingId = await service.CrearAsync(Sale());
        Check((await service.ObtenerPorIdAsync(pendingId))!.ResultadoAfip == "PENDIENTE", "SQL: corte indeterminado queda persistido");
        await Throws(() => service.CrearAsync(Sale()), "SQL: pendiente bloquea siguiente número de su serie");
        remote.Mode = "ok";
        await service.ReintentarAfipAsync(pendingId);
        Check((await service.ObtenerPorIdAsync(pendingId))!.EnviadaAfip && remote.Sent == 3, "SQL: reintento primero consulta y recupera");
        remote.Mode = "reject";
        var rejectedId = await service.CrearAsync(Sale());
        Check((await service.ObtenerPorIdAsync(rejectedId))!.ResultadoAfip == "R", "SQL: rechazo no figura como emitida");
        var count = await setup.CajaMovimientos.CountAsync();
        remote.Mode = "ok";
        await service.ReintentarAfipAsync(rejectedId);
        Check((await service.ObtenerPorIdAsync(rejectedId))!.EnviadaAfip && await setup.CajaMovimientos.CountAsync() == count + 1, "SQL: rechazo se puede reintentar sin duplicar caja");
        var credit = Sale(); credit.TipoComprobante = TipoComprobante.NotaCredito; credit.IdFacturaAsociada = id;
        var creditId = await service.CrearAsync(credit);
        Check((await service.ObtenerPorIdAsync(creditId))!.EnviadaAfip && remote.LastCode == 8, "SQL: nota de crédito B asociada solicita código 8");
        var over = Sale(); over.TipoComprobante = TipoComprobante.NotaCredito; over.IdFacturaAsociada = id;
        await Throws(() => service.CrearAsync(over), "SQL: no acreditar más que el total original");
        var sentBeforeX = remote.Sent;
        var cajaBeforeX = await setup.CajaMovimientos.CountAsync();
        var mercadoPago = Sale();
        mercadoPago.EnviarAfip = false;
        mercadoPago.MontoRecibido = mercadoPago.Total;
        mercadoPago.Pagos = new() { new() { FormaPago = FormaPago.MercadoPago, Monto = mercadoPago.Total } };
        var mpId = await service.CrearAsync(mercadoPago);
        var mpSaved = await service.ObtenerPorIdAsync(mpId);
        Check(mpSaved!.TipoComprobante == TipoComprobante.FacturaX && !mpSaved.EnviadaAfip &&
            mpSaved.Pagos.Single().FormaPago == FormaPago.MercadoPago, "SQL: ARCA desmarcado guarda Factura X con Mercado Pago");
        Check(remote.Sent == sentBeforeX && await setup.CajaMovimientos.CountAsync() == cajaBeforeX,
            "SQL: Factura X Mercado Pago no envía a ARCA ni ingresa efectivo");
        var efectivoX = Sale(); efectivoX.EnviarAfip = false; efectivoX.MontoRecibido = efectivoX.Total + 100;
        var xId = await service.CrearAsync(efectivoX);
        var xSaved = await service.ObtenerPorIdAsync(xId);
        Check(xSaved!.TipoComprobante == TipoComprobante.FacturaX && xSaved.MontoRecibido - xSaved.Total == 100,
            "SQL: efectivo recibido se conserva para calcular el vuelto");
        Check(await setup.CajaMovimientos.CountAsync() == cajaBeforeX + 1 && remote.Sent == sentBeforeX,
            "SQL: Factura X en efectivo registra caja sin llamar a ARCA");
        await service.CrearAsync(efectivoX);
        Check(await setup.CajaMovimientos.CountAsync() == cajaBeforeX + 1, "SQL: reintentar Factura X no duplica ingreso de caja");
        await service.AnularAsync(xId);
        Check((await service.ObtenerPorIdAsync(xId))!.Estado == EstadoFactura.Anulada, "SQL: Factura X se anula localmente y revierte su efectivo");
        options.Ambiente = "Homologacion";
        remote = new FakeWsfe();
        service = new FacturaService(factory, remote, new ValidConfiguration(), Options.Create(options));
        var beforeCash = await setup.CajaMovimientos.CountAsync();
        var testId = await service.CrearAsync(Sale());
        Check((await service.ObtenerPorIdAsync(testId))!.EnviadaAfip && await setup.CajaMovimientos.CountAsync() == beforeCash, "SQL: homologación no afecta caja y permite su propia serie");
        var local = Sale(); local.TipoComprobante = TipoComprobante.Presupuesto; local.EnviarAfip = false;
        var localId = await service.CrearAsync(local);
        await service.AnularAsync(localId);
        Check((await service.ObtenerPorIdAsync(localId))!.Estado == EstadoFactura.Anulada, "SQL: presupuesto se anula sin devolución ficticia");
        remote.Mode = "not-sent";
        var unsentId = await service.CrearAsync(Sale());
        var frozen = await setup.Facturas.AsNoTracking().Where(x => x.IdFactura == unsentId).Select(x => x.SolicitudAfip).SingleAsync();
        Check((await service.ObtenerPorIdAsync(unsentId))!.ResultadoAfip == "PENDIENTE", "SQL: envío que no llegó queda pendiente, no rechazado");
        remote.Mode = "ok";
        await service.ReintentarAfipAsync(unsentId);
        Check((await service.ObtenerPorIdAsync(unsentId))!.EnviadaAfip && remote.LastRequest == frozen, "SQL: ausencia confirmada reenvía la misma solicitud");
        var concurrent = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => service.CrearAsync(Sale())));
        var numbers = await setup.Facturas.Where(x => concurrent.Contains(x.IdFactura)).Select(x => x.NumeroComprobante).ToListAsync();
        Check(numbers.Distinct().Count() == 4, "SQL: emisiones simultáneas no comparten número");
        var sameOperation = Sale();
        var duplicate = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => service.CrearAsync(sameOperation)));
        Check(duplicate.Distinct().Count() == 1, "SQL: operación concurrente repetida conserva una sola factura");
    }
    finally
    {
        SqlConnection.ClearAllPools();
        // Nombre generado por este proceso; nunca se borra la base de la aplicación.
        if (!name.StartsWith("MIL_DAN_AFIP_TEST_") || name.Length != "MIL_DAN_AFIP_TEST_".Length + 32) throw new Exception("Nombre de prueba inválido.");
        await using var drop = master.CreateCommand();
        drop.CommandText = "ALTER DATABASE [" + name + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [" + name + "];";
        await drop.ExecuteNonQueryAsync();
        Console.WriteLine("Base temporal de pruebas eliminada.");
    }
}
if (args.Contains("--arca"))
{
    using var cfg = JsonDocument.Parse(File.ReadAllText(configPath));
    var liveOptions = JsonSerializer.Deserialize<AfipOptions>(cfg.RootElement.GetProperty("Afip").GetRawText())!;
    if (liveOptions.Ambiente != "Homologacion") throw new Exception("Las pruebas reales solo se permiten en homologación.");
    var liveConfiguration = new AfipConfiguracionService(Options.Create(liveOptions));
    Check(liveConfiguration.ValidarConfiguracion().ConfiguracionValida, "Configuración y firma de certificado real");
    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    var loginService = new AfipWsaaService(Options.Create(liveOptions), http, liveConfiguration);
    var login = await loginService.ObtenerLoginTicketAsync();
    if (!login.Exitoso) { Console.WriteLine("Prueba remota pendiente: " + login.Error); Environment.ExitCode = 2; }
    else
    {
        var live = new AfipWsfeService(Options.Create(liveOptions), loginService, http);
        foreach (var code in new[] { 6, 1 })
        {
            var last = await live.ObtenerUltimoComprobanteAutorizadoAsync(8, code);
            Check(last.Exitoso, "Consulta remota tipo " + code + (last.Error is null ? "" : ": " + last.Error));
            invoice.CodigoComprobanteAfip = code; invoice.NumeroComprobante = last.ProximoNumero;
            invoice.Fecha = AfipSolicitudFactory.FechaArgentina();
            client.RegimenImpositivo = code == 1 ? TipoRegimenImpositivo.ResponsableInscripto : TipoRegimenImpositivo.ConsumidorFinal;
            client.Cuit = code == 1 ? liveOptions.Cuit : null;
            var xml = AfipSolicitudFactory.Crear(invoice, client, null, liveOptions);
            var result = await live.AutorizarAsync(xml);
            if (!result.Autorizado) throw new Exception("Emisión homologación tipo " + code + ": " + result.Error);
            Check((await live.ConsultarAsync(xml)).Autorizado, "CAE real y recuperación tipo " + code);
        }
    }
}
if (args.Contains("--produccion-check"))
{
    using var cfg = JsonDocument.Parse(File.ReadAllText(configPath));
    var liveOptions = JsonSerializer.Deserialize<AfipOptions>(cfg.RootElement.GetProperty("Afip").GetRawText())!;
    if (liveOptions.Ambiente != "Produccion" || liveOptions.WsaaUrl != "https://wsaa.afip.gov.ar/ws/services/LoginCms" ||
        liveOptions.WsfeUrl != "https://servicios1.afip.gov.ar/wsfev1/service.asmx") throw new Exception("Se requieren los endpoints de producción.");
    var realConfiguration = new AfipConfiguracionService(Options.Create(liveOptions));
    var state = realConfiguration.ValidarConfiguracion();
    Check(state.ConfiguracionValida, "Configuración de producción: " + string.Join(" ", state.Errores));
    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    var realWsaa = new AfipWsaaService(Options.Create(liveOptions), http, realConfiguration);
    var login = await realWsaa.ObtenerLoginTicketAsync();
    Check(login.Exitoso, "Login WSAA producción" + (login.Error is null ? "" : ": " + login.Error));
    var realWsfe = new AfipWsfeService(Options.Create(liveOptions), realWsaa, http);
    var pointError = await realWsfe.ValidarPuntoVentaAsync();
    Check(pointError is null, "Punto de venta 8 habilitado para CAE" + (pointError is null ? "" : ": " + pointError));
    foreach (var code in new[] { 6, 1 })
    {
        var last = await realWsfe.ObtenerUltimoComprobanteAutorizadoAsync(liveOptions.PuntoVenta, code);
        Check(last.Exitoso, "Consulta WSFE producción tipo " + code + (last.Error is null ? "" : ": " + last.Error));
        Console.WriteLine("Tipo " + code + ": último autorizado " + last.UltimoNumero + ", próximo " + last.ProximoNumero);
    }
    Console.WriteLine("Verificación de producción terminada. NO se emitieron comprobantes.");
}
Console.WriteLine(tests + " comprobaciones aprobadas.");

sealed class FakeHttp(string response) : HttpMessageHandler
{
    public string Response = response;
    public HttpStatusCode Status = HttpStatusCode.OK;
    public string? LastBody;
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastBody = await request.Content!.ReadAsStringAsync(cancellationToken);
        return new(Status) { Content = new StringContent(Response, Encoding.UTF8, "text/xml") };
    }
}
sealed class FakeLogin : IAfipWsaaService
{
    public Task<AfipLoginResultado> ObtenerLoginTicketAsync() => Task.FromResult(new AfipLoginResultado { Exitoso = true, Token = "token<&", Sign = "sign" });
}
sealed class ValidConfiguration : IAfipConfiguracionService
{
    public AfipEstadoConfiguracionViewModel ValidarConfiguracion() => new() { ConfiguracionValida = true };
}
sealed class TestFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext() => new(options);
}
sealed class FakeWsfe : IAfipWsfeService
{
    public string Mode = "ok";
    public int Sent;
    public int LastCode;
    public string? LastRequest;
    private readonly Dictionary<int, int> _last = new();
    private readonly Dictionary<string, AfipAutorizacionResultado> _results = new();
    public Task<AfipUltimoComprobanteResultado> ObtenerUltimoComprobanteAutorizadoAsync(int point, int code) =>
        Task.FromResult(new AfipUltimoComprobanteResultado { Exitoso = true, PuntoVenta = point, CodigoTipoComprobanteAfip = code, UltimoNumero = _last.GetValueOrDefault(code) });
    public Task<AfipAutorizacionResultado> AutorizarAsync(string xml)
    {
        Sent++;
        LastRequest = xml;
        var request = XElement.Parse(xml);
        LastCode = int.Parse(request.Descendants().Single(x => x.Name.LocalName == "CbteTipo").Value);
        if (Mode == "reject") return Task.FromResult(new AfipAutorizacionResultado { Rechazado = true, Error = "Rechazo de prueba" });
        if (Mode == "not-sent") return Task.FromResult(new AfipAutorizacionResultado { Error = "Corte antes de llegar" });
        var number = int.Parse(request.Descendants().Single(x => x.Name.LocalName == "CbteDesde").Value);
        _last[LastCode] = number;
        var result = new AfipAutorizacionResultado { Autorizado = true, Cae = "12345678901234", Vencimiento = DateTime.Today.AddDays(10) };
        _results[xml] = result;
        return Task.FromResult(Mode is "lost" or "pending" ? new AfipAutorizacionResultado { Error = "Corte de red" } : result);
    }
    public Task<AfipAutorizacionResultado> ConsultarAsync(string xml) => Task.FromResult(
        Mode == "pending" ? new AfipAutorizacionResultado { Error = "Consulta temporalmente caída" } :
        _results.TryGetValue(xml, out var result) ? result : new AfipAutorizacionResultado { NoEncontrado = true });
}
