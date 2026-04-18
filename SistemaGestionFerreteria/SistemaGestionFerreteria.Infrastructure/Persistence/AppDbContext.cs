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
        public DbSet<ProductoPrecio> ProductosPrecios => Set<ProductoPrecio>();
        public DbSet<UnidadMedida> UnidadesMedida => Set<UnidadMedida>();
        public DbSet<Parametro> Parametros => Set<Parametro>();

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
        }
    }
}