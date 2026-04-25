using Microsoft.EntityFrameworkCore;
using SistemaGestionFerreteria.Domain.Entities;
using SistemaGestionFerreteria.Domain.Enums;

namespace SistemaGestionFerreteria.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public DbSet<Producto> Productos => Set<Producto>();
        public DbSet<Categoria> Categorias => Set<Categoria>();
        public DbSet<Proveedor> Proveedores => Set<Proveedor>();
        public DbSet<Cliente> Clientes => Set<Cliente>();
        public DbSet<ProductoPrecio> ProductosPrecios => Set<ProductoPrecio>();
        public DbSet<UnidadMedida> UnidadesMedida => Set<UnidadMedida>();
        public DbSet<Parametro> Parametros => Set<Parametro>();
        public DbSet<Factura> Facturas => Set<Factura>();
        public DbSet<FacturaDetalle> FacturaDetalles => Set<FacturaDetalle>();
        public DbSet<FacturaPago> FacturaPagos => Set<FacturaPago>();

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Categoria>(entity =>
            {
                entity.ToTable("Categorias");

                entity.HasKey(x => x.IdCategoria);  
                entity.Property(x => x.IdCategoria)
                    .ValueGeneratedOnAdd();

                entity.Property(x => x.Descripcion)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(x => x.Activo)
                    .HasDefaultValue(true);
                
                entity.Property(x => x.IdCategoria)
                    .IsRequired();
            });

            modelBuilder.Entity<Cliente>(entity =>
            {
                entity.ToTable("Clientes");

                entity.HasKey(x => x.IdCliente);

                entity.Property(x => x.IdCliente)
                    .ValueGeneratedOnAdd();

                entity.Property(x => x.Nombre)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.Apellido)
                    .HasMaxLength(100);

                entity.Property(x => x.Documento)
                    .HasMaxLength(50);

                entity.HasIndex(x => x.Documento)
                    .IsUnique()
                    .HasFilter("[Documento] IS NOT NULL AND [Documento] <> ''");

                entity.Property(x => x.Telefono)
                    .HasMaxLength(50);

                entity.Property(x => x.Email)
                    .HasMaxLength(150);

                entity.Property(x => x.Direccion)
                    .HasMaxLength(250);

                entity.Property(x => x.RequiereFactura)
                    .HasDefaultValue(false);

                entity.Property(x => x.Cuit)
                    .HasMaxLength(50);

                entity.Property(x => x.RegimenImpositivo)
                    .HasConversion<int>()
                    .IsRequired();

                entity.Property(x => x.Activo)
                    .HasDefaultValue(true);

                entity.Property(x => x.FechaAlta)
                    .HasDefaultValueSql("GETDATE()");
            });

            modelBuilder.Entity<Proveedor>(entity =>
            {
                entity.ToTable("Proveedores");

                entity.HasKey(x => x.IdProveedor);

                entity.Property(x => x.IdProveedor)
                    .ValueGeneratedOnAdd();

                entity.Property(x => x.Descripcion)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.Telefono)
                    .HasMaxLength(50);

                entity.Property(x => x.Email)
                    .HasMaxLength(150);

                entity.Property(x => x.Direccion)
                    .HasMaxLength(250);

                entity.Property(x => x.Activo)
                    .HasDefaultValue(true);
            });

            modelBuilder.Entity<Producto>(entity =>
            {
                entity.ToTable("Productos");

                entity.HasKey(x => x.IdProducto);

                entity.Property(x => x.IdProducto)
                    .ValueGeneratedOnAdd();

                entity.Property(x => x.CodigoProducto)
                    .HasMaxLength(50);

                entity.HasIndex(x => x.CodigoProducto)
                    .IsUnique()
                    .HasFilter("[CodigoProducto] IS NOT NULL AND [CodigoProducto] <> ''");

                entity.Property(x => x.CodigoBarra)
                    .HasMaxLength(50);

                entity.Property(x => x.Descripcion)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.Observacion)
                    .HasMaxLength(500);

                entity.Property(x => x.StockActual)
                    .HasColumnType("decimal(18,2)");

                entity.Property(x => x.StockMinimo)
                    .HasColumnType("decimal(18,2)");

                entity.Property(x => x.LlevaStock)
                    .HasDefaultValue(true);

                entity.Property(x => x.Activo)
                    .HasDefaultValue(true);

                entity.Property(x => x.FechaAlta)
                    .HasDefaultValueSql("GETDATE()");

                entity.HasOne(x => x.Categoria)
                    .WithMany(x => x.Productos)
                    .HasForeignKey(x => x.IdCategoria)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Proveedor)
                    .WithMany(x => x.Productos)
                    .HasForeignKey(x => x.IdProveedor)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Producto>()
                .HasOne(x => x.UnidadMedida)
                .WithMany(x => x.Productos)
                .HasForeignKey(x => x.IdUnidadMedida)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductoPrecio>(entity =>
            {
                entity.ToTable("ProductosPrecios");

                entity.HasKey(x => x.IdProductoPrecio);

                entity.Property(x => x.IdProductoPrecio)
                    .ValueGeneratedOnAdd();

                entity.Property(x => x.PrecioCosto)
                    .HasColumnType("decimal(18,2)");

                entity.Property(x => x.PorcentajeGanancia)
                    .HasColumnType("decimal(9,2)");

                entity.Property(x => x.PrecioVenta)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                entity.Property(x => x.PrecioEspecial)
                    .HasColumnType("decimal(18,2)");

                entity.Property(x => x.TipoIva)
                    .HasConversion<int>()
                    .IsRequired();

                entity.Property(x => x.FechaDesde)
                    .HasDefaultValueSql("GETDATE()");

                entity.Property(x => x.UsuarioAlta)
                    .HasMaxLength(100);

                entity.HasOne(x => x.Producto)
                    .WithMany(x => x.Precios)
                    .HasForeignKey(x => x.IdProducto)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UnidadMedida>()
                .HasKey(x => x.IdUnidadMedida);
            modelBuilder.Entity<UnidadMedida>().ToTable("UnidadesMedida");

            modelBuilder.Entity<Parametro>(entity =>
            {
                entity.ToTable("Parametros");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Codigo)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.HasIndex(x => x.Codigo)
                    .IsUnique();

                entity.Property(x => x.Valor)
                    .HasMaxLength(1000)
                    .IsRequired();

                entity.Property(x => x.Grupo)
                    .HasMaxLength(100);

                entity.Property(x => x.Descripcion)
                    .HasMaxLength(250);

                entity.Property(x => x.Tipo)
                    .HasConversion<int>();

                entity.Property(x => x.Activo)
                    .HasDefaultValue(true);
            });

            //Facturas y detalles
            modelBuilder.Entity<Factura>(entity =>
            {
                entity.ToTable("Facturas");

                entity.HasKey(x => x.IdFactura);

                entity.Property(x => x.IdFactura)
                    .ValueGeneratedOnAdd();

                entity.Property(x => x.TipoComprobante)
                    .HasConversion<int>()
                    .IsRequired();

                entity.Property(x => x.PuntoVenta)
                    .IsRequired();

                entity.Property(x => x.NumeroComprobante)
                    .IsRequired();

                entity.Property(x => x.Fecha)
                    .HasDefaultValueSql("GETDATE()");

                entity.Property(x => x.Estado)
                    .HasConversion<int>()
                    .HasDefaultValue(EstadoFactura.Borrador);

                entity.Property(x => x.CondicionVenta)
                    .HasConversion<int>()
                    .IsRequired();

                entity.Property(x => x.Observacion)
                    .HasMaxLength(500);

                entity.Property(x => x.Subtotal)
                    .HasColumnType("decimal(18,2)");

                entity.Property(x => x.TotalIva21)
                    .HasColumnType("decimal(18,2)");

                entity.Property(x => x.TotalIva105)
                    .HasColumnType("decimal(18,2)");

                entity.Property(x => x.TotalIva27)
                    .HasColumnType("decimal(18,2)");

                entity.Property(x => x.TotalExento)
                    .HasColumnType("decimal(18,2)");

                entity.Property(x => x.Total)
                    .HasColumnType("decimal(18,2)");

                entity.Property(x => x.EnviadaAfip)
                    .HasDefaultValue(false);

                entity.Property(x => x.Cae)
                    .HasMaxLength(50);

                entity.Property(x => x.ResultadoAfip)
                    .HasMaxLength(500);

                entity.Property(x => x.Activo)
                    .HasDefaultValue(true);

                entity.Property(x => x.FechaAlta)
                    .HasDefaultValueSql("GETDATE()");

                entity.HasIndex(x => new { x.TipoComprobante, x.PuntoVenta, x.NumeroComprobante })
                    .IsUnique()
                    .HasDatabaseName("IX_Facturas_Numero");

                entity.HasOne(x => x.Cliente)
                    .WithMany(x => x.Facturas)
                    .HasForeignKey(x => x.IdCliente)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<FacturaDetalle>(entity =>
            {
                entity.ToTable("FacturaDetalles");

                entity.HasKey(x => x.IdFacturaDetalle);

                entity.Property(x => x.IdFacturaDetalle)
                    .ValueGeneratedOnAdd();

                entity.Property(x => x.CodigoProducto)
                    .HasMaxLength(50);

                entity.Property(x => x.Descripcion)
                    .HasMaxLength(300)
                    .IsRequired();

                entity.Property(x => x.Cantidad)
                    .HasColumnType("decimal(18,2)");

                entity.Property(x => x.PrecioUnitario)
                    .HasColumnType("decimal(18,2)");

                entity.Property(x => x.PorcentajeIva)
                    .HasColumnType("decimal(5,2)");

                entity.Property(x => x.ImporteIva)
                    .HasColumnType("decimal(18,2)");

                entity.Property(x => x.Subtotal)
                    .HasColumnType("decimal(18,2)");

                entity.HasOne(x => x.Factura)
                    .WithMany(x => x.Detalles)
                    .HasForeignKey(x => x.IdFactura)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Producto)
                    .WithMany(x => x.FacturaDetalles)
                    .HasForeignKey(x => x.IdProducto)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<FacturaPago>(entity =>
            {
                entity.ToTable("FacturaPagos");

                entity.HasKey(x => x.IdFacturaPago);

                entity.Property(x => x.IdFacturaPago)
                    .ValueGeneratedOnAdd();

                entity.Property(x => x.FormaPago)
                    .HasConversion<int>()
                    .IsRequired();

                entity.Property(x => x.Monto)
                    .HasColumnType("decimal(18,2)");

                entity.Property(x => x.Referencia)
                    .HasMaxLength(100);

                entity.HasOne(x => x.Factura)
                    .WithMany(x => x.Pagos)
                    .HasForeignKey(x => x.IdFactura)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}