// Infrastructure/Services/Facturacion/FacturaService.cs

using Microsoft.EntityFrameworkCore;
using SistemaGestionFerreteria.Application.Features.Facturacion.Models;
using SistemaGestionFerreteria.Application.Interfaces;
using SistemaGestionFerreteria.Domain.Entities;
using SistemaGestionFerreteria.Domain.Enums;
using SistemaGestionFerreteria.Infrastructure.Persistence;

namespace SistemaGestionFerreteria.Infrastructure.Services.Facturacion
{
    public class FacturaService : IFacturaService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public FacturaService(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<int> ObtenerProximoNumeroAsync(TipoComprobante tipoComprobante, int puntoVenta)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var ultimoNumero = await context.Facturas
                .Where(x => x.TipoComprobante == tipoComprobante
                         && x.PuntoVenta == puntoVenta)
                .Select(x => (int?)x.NumeroComprobante)
                .MaxAsync();

            return (ultimoNumero ?? 0) + 1;
        }

        public async Task<int> CrearAsync(FacturaViewModel model)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var factura = new Factura
            {
                IdCliente = model.IdCliente ?? 0,
                TipoComprobante = model.TipoComprobante,
                PuntoVenta = model.PuntoVenta,
                NumeroComprobante = model.NumeroComprobante,
                Fecha = DateTime.Now,
                Estado = EstadoFactura.Emitida,
                CondicionVenta = model.CondicionVenta,
                Observacion = model.Observacion,

                Subtotal = model.Subtotal,
                TotalIva21 = model.TotalIva21,
                TotalIva105 = model.TotalIva105,
                TotalIva27 = model.TotalIva27,
                TotalExento = model.TotalExento,
                Total = model.Total,

                EnviadaAfip = false,
                Activo = true,

                Detalles = model.Detalles.Select(x => new FacturaDetalle
                {
                    IdProducto = x.IdProducto ?? 0,
                    CodigoProducto = x.CodigoProducto,
                    Descripcion = x.Descripcion,
                    Cantidad = x.Cantidad,
                    PrecioUnitario = x.PrecioUnitario,
                    PorcentajeIva = x.PorcentajeIva,
                    ImporteIva = x.ImporteIva,
                    Subtotal = x.Subtotal
                }).ToList(),

                Pagos = model.Pagos.Select(x => new FacturaPago
                {
                    FormaPago = x.FormaPago,
                    Monto = x.Monto,
                    Referencia = x.Referencia
                }).ToList()
            };

            context.Facturas.Add(factura);


            await context.SaveChangesAsync();

            return factura.IdFactura;
        }

        public async Task<List<FacturaListadoViewModel>> ObtenerTodasAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Facturas
                .AsNoTracking()
                .Include(x => x.Cliente)
                .OrderByDescending(x => x.Fecha)
                .Select(x => new FacturaListadoViewModel
                {
                    IdFactura = x.IdFactura,
                    Fecha = x.Fecha,
                    TipoComprobante = x.TipoComprobante,
                    PuntoVenta = x.PuntoVenta,
                    NumeroComprobante = x.NumeroComprobante,
                    Cliente = x.Cliente.Nombre + (string.IsNullOrWhiteSpace(x.Cliente.Apellido)
                        ? ""
                        : " " + x.Cliente.Apellido),
                    Total = x.Total,
                    Estado = x.Estado
                })
                .ToListAsync();
        }

        public async Task<FacturaViewModel?> ObtenerPorIdAsync(int idFactura)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Facturas
                .AsNoTracking()
                .Include(x => x.Detalles)
                .Include(x => x.Pagos)
                .Where(x => x.IdFactura == idFactura)
                .Select(x => new FacturaViewModel
                {
                    IdFactura = x.IdFactura,
                    IdCliente = x.IdCliente,
                    TipoComprobante = x.TipoComprobante,
                    PuntoVenta = x.PuntoVenta,
                    NumeroComprobante = x.NumeroComprobante,
                    CondicionVenta = x.CondicionVenta,
                    Observacion = x.Observacion,

                    Subtotal = x.Subtotal,
                    TotalIva21 = x.TotalIva21,
                    TotalIva105 = x.TotalIva105,
                    TotalIva27 = x.TotalIva27,
                    TotalExento = x.TotalExento,
                    Total = x.Total,

                    Detalles = x.Detalles.Select(d => new FacturaDetalleViewModel
                    {
                        IdProducto = d.IdProducto,
                        CodigoProducto = d.CodigoProducto,
                        Descripcion = d.Descripcion,
                        Cantidad = d.Cantidad,
                        PrecioUnitario = d.PrecioUnitario,
                        PorcentajeIva = d.PorcentajeIva,
                        ImporteIva = d.ImporteIva,
                        Subtotal = d.Subtotal
                    }).ToList(),

                    Pagos = x.Pagos.Select(p => new FacturaPagoViewModel
                    {
                        FormaPago = p.FormaPago,
                        Monto = p.Monto,
                        Referencia = p.Referencia
                    }).ToList()
                })
                .FirstOrDefaultAsync();
        }
    }
}