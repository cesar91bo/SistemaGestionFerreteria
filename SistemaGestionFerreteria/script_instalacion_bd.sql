IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Categorias] (
    [IdCategoria] int NOT NULL IDENTITY,
    [Descripcion] nvarchar(150) NOT NULL,
    [Activo] bit NOT NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK_Categorias] PRIMARY KEY ([IdCategoria])
);
GO

CREATE TABLE [Proveedores] (
    [IdProveedor] int NOT NULL IDENTITY,
    [Descripcion] nvarchar(100) NOT NULL,
    [Telefono] nvarchar(50) NULL,
    [Email] nvarchar(150) NULL,
    [Direccion] nvarchar(250) NULL,
    [Activo] bit NOT NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK_Proveedores] PRIMARY KEY ([IdProveedor])
);
GO

CREATE TABLE [Productos] (
    [IdProducto] int NOT NULL IDENTITY,
    [CodigoProducto] nvarchar(50) NOT NULL,
    [CodigoBarra] nvarchar(50) NULL,
    [Descripcion] nvarchar(200) NOT NULL,
    [Observacion] nvarchar(500) NULL,
    [IdCategoria] int NOT NULL,
    [IdProveedor] int NULL,
    [IdUnidadMedida] int NOT NULL,
    [LlevaStock] bit NOT NULL DEFAULT CAST(1 AS bit),
    [StockActual] decimal(18,2) NULL,
    [StockMinimo] decimal(18,2) NULL,
    [Activo] bit NOT NULL DEFAULT CAST(1 AS bit),
    [FechaAlta] datetime2 NOT NULL DEFAULT (GETDATE()),
    [FechaModificacion] datetime2 NULL,
    [FechaBaja] datetime2 NULL,
    CONSTRAINT [PK_Productos] PRIMARY KEY ([IdProducto]),
    CONSTRAINT [FK_Productos_Categorias_IdCategoria] FOREIGN KEY ([IdCategoria]) REFERENCES [Categorias] ([IdCategoria]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Productos_Proveedores_IdProveedor] FOREIGN KEY ([IdProveedor]) REFERENCES [Proveedores] ([IdProveedor]) ON DELETE NO ACTION
);
GO

CREATE TABLE [ProductosPrecios] (
    [IdProductoPrecio] int NOT NULL IDENTITY,
    [IdProducto] int NOT NULL,
    [PrecioCosto] decimal(18,2) NOT NULL,
    [PrecioVenta] decimal(18,2) NOT NULL,
    [PrecioEspecial] decimal(18,2) NULL,
    [PorcentajeIva] decimal(5,2) NULL,
    [FechaDesde] datetime2 NOT NULL DEFAULT (GETDATE()),
    [FechaHasta] datetime2 NULL,
    [UsuarioAlta] nvarchar(100) NULL,
    CONSTRAINT [PK_ProductosPrecios] PRIMARY KEY ([IdProductoPrecio]),
    CONSTRAINT [FK_ProductosPrecios_Productos_IdProducto] FOREIGN KEY ([IdProducto]) REFERENCES [Productos] ([IdProducto]) ON DELETE CASCADE
);
GO

CREATE UNIQUE INDEX [IX_Productos_CodigoProducto] ON [Productos] ([CodigoProducto]);
GO

CREATE INDEX [IX_Productos_IdCategoria] ON [Productos] ([IdCategoria]);
GO

CREATE INDEX [IX_Productos_IdProveedor] ON [Productos] ([IdProveedor]);
GO

CREATE INDEX [IX_ProductosPrecios_IdProducto] ON [ProductosPrecios] ([IdProducto]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260412185619_Inicial', N'8.0.25');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [UnidadMedida] (
    [IdUnidadMedida] int NOT NULL IDENTITY,
    [Descripcion] nvarchar(max) NOT NULL,
    [Abreviatura] nvarchar(max) NULL,
    [Activo] bit NOT NULL,
    CONSTRAINT [PK_UnidadMedida] PRIMARY KEY ([IdUnidadMedida])
);
GO

CREATE INDEX [IX_Productos_IdUnidadMedida] ON [Productos] ([IdUnidadMedida]);
GO

ALTER TABLE [Productos] ADD CONSTRAINT [FK_Productos_UnidadMedida_IdUnidadMedida] FOREIGN KEY ([IdUnidadMedida]) REFERENCES [UnidadMedida] ([IdUnidadMedida]) ON DELETE NO ACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260415041010_AgregaUnidadMedida', N'8.0.25');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Productos]') AND [c].[name] = N'IdUnidadMedida');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Productos] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [Productos] ALTER COLUMN [IdUnidadMedida] int NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260415042738_Fix_IdUnidadMedidaNullable', N'8.0.25');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Productos]') AND [c].[name] = N'IdUnidadMedida');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Productos] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [Productos] ALTER COLUMN [IdUnidadMedida] int NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260415044238_Fix_CodigoProductoOpcional', N'8.0.25');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DROP INDEX [IX_Productos_CodigoProducto] ON [Productos];
GO

DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Productos]') AND [c].[name] = N'CodigoProducto');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Productos] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [Productos] ALTER COLUMN [CodigoProducto] nvarchar(50) NULL;
GO

CREATE UNIQUE INDEX [IX_Productos_CodigoProducto] ON [Productos] ([CodigoProducto]) WHERE [CodigoProducto] IS NOT NULL AND [CodigoProducto] <> '';
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260415044708_FixCodigoProductoNullable2', N'8.0.25');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Productos] DROP CONSTRAINT [FK_Productos_UnidadMedida_IdUnidadMedida];
GO

ALTER TABLE [UnidadMedida] DROP CONSTRAINT [PK_UnidadMedida];
GO

EXEC sp_rename N'[UnidadMedida]', N'UnidadesMedida';
GO

ALTER TABLE [UnidadesMedida] ADD CONSTRAINT [PK_UnidadesMedida] PRIMARY KEY ([IdUnidadMedida]);
GO

ALTER TABLE [Productos] ADD CONSTRAINT [FK_Productos_UnidadesMedida_IdUnidadMedida] FOREIGN KEY ([IdUnidadMedida]) REFERENCES [UnidadesMedida] ([IdUnidadMedida]) ON DELETE NO ACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260415071618_CrearUnidadMedida', N'8.0.25');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [ProductosPrecios] ADD [TipoIva] int NOT NULL DEFAULT 210;
GO


                UPDATE ProductosPrecios
                SET TipoIva =
                    CASE
                        WHEN PorcentajeIva = 10.5 THEN 105
                        WHEN PorcentajeIva = 21 THEN 210
                        WHEN PorcentajeIva = 27 THEN 270
                        ELSE 0
                    END
            
GO

DECLARE @var3 sysname;
SELECT @var3 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ProductosPrecios]') AND [c].[name] = N'PorcentajeIva');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [ProductosPrecios] DROP CONSTRAINT [' + @var3 + '];');
ALTER TABLE [ProductosPrecios] DROP COLUMN [PorcentajeIva];
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260416053839_CambiarIvaAEnum', N'8.0.25');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DECLARE @var4 sysname;
SELECT @var4 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ProductosPrecios]') AND [c].[name] = N'PrecioCosto');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [ProductosPrecios] DROP CONSTRAINT [' + @var4 + '];');
ALTER TABLE [ProductosPrecios] ALTER COLUMN [PrecioCosto] decimal(18,2) NULL;
GO

ALTER TABLE [ProductosPrecios] ADD [PorcentajeGanancia] decimal(5,2) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260416214507_AgregarPorcentajeGananciaYCostoOpcional', N'8.0.25');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DECLARE @var5 sysname;
SELECT @var5 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ProductosPrecios]') AND [c].[name] = N'TipoIva');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [ProductosPrecios] DROP CONSTRAINT [' + @var5 + '];');
GO

CREATE TABLE [Parametros] (
    [Id] int NOT NULL IDENTITY,
    [Codigo] nvarchar(100) NOT NULL,
    [Valor] nvarchar(1000) NOT NULL,
    [Grupo] nvarchar(100) NULL,
    [Descripcion] nvarchar(250) NULL,
    [Tipo] int NOT NULL,
    [FechaCreacion] datetime2 NOT NULL,
    [FechaModificacion] datetime2 NULL,
    [Activo] bit NOT NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK_Parametros] PRIMARY KEY ([Id])
);
GO

CREATE UNIQUE INDEX [IX_Parametros_Codigo] ON [Parametros] ([Codigo]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260417000336_CrearTablaParametros', N'8.0.25');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DECLARE @var6 sysname;
SELECT @var6 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ProductosPrecios]') AND [c].[name] = N'PorcentajeGanancia');
IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [ProductosPrecios] DROP CONSTRAINT [' + @var6 + '];');
ALTER TABLE [ProductosPrecios] ALTER COLUMN [PorcentajeGanancia] decimal(9,2) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260417011409_AjustarPorcentajeGananciaProductoPrecio', N'8.0.25');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Clientes] (
    [IdCliente] int NOT NULL IDENTITY,
    [Nombre] nvarchar(100) NOT NULL,
    [Apellido] nvarchar(100) NULL,
    [Documento] nvarchar(50) NULL,
    [Telefono] nvarchar(50) NULL,
    [Email] nvarchar(150) NULL,
    [Direccion] nvarchar(250) NULL,
    [Activo] bit NOT NULL DEFAULT CAST(1 AS bit),
    [FechaAlta] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_Clientes] PRIMARY KEY ([IdCliente])
);
GO

CREATE UNIQUE INDEX [IX_Clientes_Documento] ON [Clientes] ([Documento]) WHERE [Documento] IS NOT NULL AND [Documento] <> '';
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260418141821_CrearTablaClientes', N'8.0.25');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Clientes] ADD [Cuit] nvarchar(50) NULL;
GO

ALTER TABLE [Clientes] ADD [RegimenImpositivo] int NOT NULL DEFAULT 0;
GO

ALTER TABLE [Clientes] ADD [RequiereFactura] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260418144252_AgregarRegimenYCuitClientes', N'8.0.25');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Facturas] (
    [IdFactura] int NOT NULL IDENTITY,
    [IdCliente] int NOT NULL,
    [TipoComprobante] int NOT NULL,
    [PuntoVenta] int NOT NULL,
    [NumeroComprobante] int NOT NULL,
    [Fecha] datetime2 NOT NULL DEFAULT (GETDATE()),
    [FechaVencimiento] datetime2 NULL,
    [Estado] int NOT NULL DEFAULT 0,
    [CondicionVenta] int NOT NULL,
    [Observacion] nvarchar(500) NULL,
    [Subtotal] decimal(18,2) NOT NULL,
    [TotalIva21] decimal(18,2) NOT NULL,
    [TotalIva105] decimal(18,2) NOT NULL,
    [TotalIva27] decimal(18,2) NOT NULL,
    [TotalExento] decimal(18,2) NOT NULL,
    [Total] decimal(18,2) NOT NULL,
    [EnviadaAfip] bit NOT NULL DEFAULT CAST(0 AS bit),
    [Cae] nvarchar(50) NULL,
    [FechaVencimientoCae] datetime2 NULL,
    [ResultadoAfip] nvarchar(500) NULL,
    [Activo] bit NOT NULL DEFAULT CAST(1 AS bit),
    [FechaAlta] datetime2 NOT NULL DEFAULT (GETDATE()),
    [FechaModificacion] datetime2 NULL,
    CONSTRAINT [PK_Facturas] PRIMARY KEY ([IdFactura]),
    CONSTRAINT [FK_Facturas_Clientes_IdCliente] FOREIGN KEY ([IdCliente]) REFERENCES [Clientes] ([IdCliente]) ON DELETE NO ACTION
);
GO

CREATE TABLE [FacturaDetalles] (
    [IdFacturaDetalle] int NOT NULL IDENTITY,
    [IdFactura] int NOT NULL,
    [IdProducto] int NOT NULL,
    [CodigoProducto] nvarchar(50) NULL,
    [Descripcion] nvarchar(300) NOT NULL,
    [Cantidad] decimal(18,2) NOT NULL,
    [PrecioUnitario] decimal(18,2) NOT NULL,
    [PorcentajeIva] decimal(5,2) NOT NULL,
    [ImporteIva] decimal(18,2) NOT NULL,
    [Subtotal] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_FacturaDetalles] PRIMARY KEY ([IdFacturaDetalle]),
    CONSTRAINT [FK_FacturaDetalles_Facturas_IdFactura] FOREIGN KEY ([IdFactura]) REFERENCES [Facturas] ([IdFactura]) ON DELETE CASCADE,
    CONSTRAINT [FK_FacturaDetalles_Productos_IdProducto] FOREIGN KEY ([IdProducto]) REFERENCES [Productos] ([IdProducto]) ON DELETE NO ACTION
);
GO

CREATE TABLE [FacturaPagos] (
    [IdFacturaPago] int NOT NULL IDENTITY,
    [IdFactura] int NOT NULL,
    [FormaPago] int NOT NULL,
    [Monto] decimal(18,2) NOT NULL,
    [Referencia] nvarchar(100) NULL,
    CONSTRAINT [PK_FacturaPagos] PRIMARY KEY ([IdFacturaPago]),
    CONSTRAINT [FK_FacturaPagos_Facturas_IdFactura] FOREIGN KEY ([IdFactura]) REFERENCES [Facturas] ([IdFactura]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_FacturaDetalles_IdFactura] ON [FacturaDetalles] ([IdFactura]);
GO

CREATE INDEX [IX_FacturaDetalles_IdProducto] ON [FacturaDetalles] ([IdProducto]);
GO

CREATE INDEX [IX_FacturaPagos_IdFactura] ON [FacturaPagos] ([IdFactura]);
GO

CREATE INDEX [IX_Facturas_IdCliente] ON [Facturas] ([IdCliente]);
GO

CREATE UNIQUE INDEX [IX_Facturas_Numero] ON [Facturas] ([TipoComprobante], [PuntoVenta], [NumeroComprobante]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260421203445_AgregarFacturacion', N'8.0.25');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DECLARE @var7 sysname;
SELECT @var7 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[FacturaDetalles]') AND [c].[name] = N'IdProducto');
IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [FacturaDetalles] DROP CONSTRAINT [' + @var7 + '];');
ALTER TABLE [FacturaDetalles] ALTER COLUMN [IdProducto] int NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260504042616_FacturaDetalle_IdProducto_Nullable', N'8.0.25');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Cajas] (
    [IdCaja] int NOT NULL IDENTITY,
    [FechaApertura] datetime2 NOT NULL DEFAULT (GETDATE()),
    [FechaCierre] datetime2 NULL,
    [FondoInicial] decimal(18,2) NOT NULL,
    [TotalIngresos] decimal(18,2) NOT NULL,
    [TotalRetiros] decimal(18,2) NOT NULL,
    [TotalVentas] decimal(18,2) NOT NULL,
    [TotalFinal] decimal(18,2) NOT NULL,
    [Estado] int NOT NULL,
    [ObservacionApertura] nvarchar(500) NULL,
    [ObservacionCierre] nvarchar(500) NULL,
    [Activo] bit NOT NULL DEFAULT CAST(1 AS bit),
    [FechaAlta] datetime2 NOT NULL DEFAULT (GETDATE()),
    [FechaModificacion] datetime2 NULL,
    CONSTRAINT [PK_Cajas] PRIMARY KEY ([IdCaja])
);
GO

CREATE TABLE [CajaMovimientos] (
    [IdCajaMovimiento] int NOT NULL IDENTITY,
    [IdCaja] int NOT NULL,
    [IdFactura] int NULL,
    [Fecha] datetime2 NOT NULL DEFAULT (GETDATE()),
    [Tipo] int NOT NULL,
    [Monto] decimal(18,2) NOT NULL,
    [Descripcion] nvarchar(300) NOT NULL,
    [Activo] bit NOT NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK_CajaMovimientos] PRIMARY KEY ([IdCajaMovimiento]),
    CONSTRAINT [FK_CajaMovimientos_Cajas_IdCaja] FOREIGN KEY ([IdCaja]) REFERENCES [Cajas] ([IdCaja]) ON DELETE CASCADE,
    CONSTRAINT [FK_CajaMovimientos_Facturas_IdFactura] FOREIGN KEY ([IdFactura]) REFERENCES [Facturas] ([IdFactura]) ON DELETE NO ACTION
);
GO

CREATE INDEX [IX_CajaMovimientos_IdCaja] ON [CajaMovimientos] ([IdCaja]);
GO

CREATE INDEX [IX_CajaMovimientos_IdFactura] ON [CajaMovimientos] ([IdFactura]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260510050235_AgregarModuloCaja', N'8.0.25');
GO

COMMIT;
GO

