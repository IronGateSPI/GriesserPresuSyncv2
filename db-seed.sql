IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;

GO

CREATE TABLE [IG_GriesserSyncPresupuestos] (
    [IdLinea] nvarchar(450) NOT NULL,
    [IdBudget] int NOT NULL,
    [date] datetime2 NOT NULL,
    [Articulo] nvarchar(max) NULL,
    [Cliente] nvarchar(max) NULL,
    [NPresupuesto] nvarchar(max) NULL,
    [NPersianas] int NOT NULL,
    [TotalSup] real NOT NULL,
    [TotalAncho] real NOT NULL,
    [TotalLargo] real NOT NULL,
    [LargoTapas] real NOT NULL,
    [TotalTapas] int NOT NULL,
    [Embalaje] nvarchar(max) NULL,
    [POS] int NOT NULL,
    [BK] real NOT NULL,
    [HL] real NOT NULL,
    [Accion] nvarchar(max) NULL,
    [TL] real NOT NULL,
    [Uni] int NOT NULL,
    [PUnidad] real NOT NULL,
    [PUnidad2] real NOT NULL,
    [TEUR] real NOT NULL,
    [POS1] nvarchar(max) NULL,
    [Color] nvarchar(max) NULL,
    [IsSincronized] bit NOT NULL,
    CONSTRAINT [PK_IG_GriesserSyncPresupuestos] PRIMARY KEY ([IdLinea])
);

GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20201117061632_InitialSeed', N'3.1.9');

GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20201211150433_CodProduct', N'3.1.9');

GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[IG_GriesserSyncPresupuestos]') AND [c].[name] = N'Cliente');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [IG_GriesserSyncPresupuestos] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [IG_GriesserSyncPresupuestos] DROP COLUMN [Cliente];

GO

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[IG_GriesserSyncPresupuestos]') AND [c].[name] = N'Embalaje');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [IG_GriesserSyncPresupuestos] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [IG_GriesserSyncPresupuestos] DROP COLUMN [Embalaje];

GO

DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[IG_GriesserSyncPresupuestos]') AND [c].[name] = N'POS1');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [IG_GriesserSyncPresupuestos] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [IG_GriesserSyncPresupuestos] DROP COLUMN [POS1];

GO

ALTER TABLE [IG_GriesserSyncPresupuestos] ADD [con_testero] nvarchar(max) NULL;

GO

ALTER TABLE [IG_GriesserSyncPresupuestos] ADD [price] real NOT NULL DEFAULT CAST(0 AS real);

GO

ALTER TABLE [IG_GriesserSyncPresupuestos] ADD [price_tapa] real NOT NULL DEFAULT CAST(0 AS real);

GO

ALTER TABLE [IG_GriesserSyncPresupuestos] ADD [price_testero] real NOT NULL DEFAULT CAST(0 AS real);

GO

ALTER TABLE [IG_GriesserSyncPresupuestos] ADD [tipo] nvarchar(max) NULL;

GO

ALTER TABLE [IG_GriesserSyncPresupuestos] ADD [title] nvarchar(max) NULL;

GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20201223101304_NuevosCampos', N'3.1.9');

GO

ALTER TABLE [IG_GriesserSyncPresupuestos] ADD [importe_automatismos] real NOT NULL DEFAULT CAST(0 AS real);

GO

ALTER TABLE [IG_GriesserSyncPresupuestos] ADD [importe_color] real NOT NULL DEFAULT CAST(0 AS real);

GO

ALTER TABLE [IG_GriesserSyncPresupuestos] ADD [importe_incrementos] real NOT NULL DEFAULT CAST(0 AS real);

GO

ALTER TABLE [IG_GriesserSyncPresupuestos] ADD [importe_lineas] real NOT NULL DEFAULT CAST(0 AS real);

GO

ALTER TABLE [IG_GriesserSyncPresupuestos] ADD [importe_tapas_y_testeros] real NOT NULL DEFAULT CAST(0 AS real);

GO

ALTER TABLE [IG_GriesserSyncPresupuestos] ADD [importe_tejido] real NOT NULL DEFAULT CAST(0 AS real);

GO

ALTER TABLE [IG_GriesserSyncPresupuestos] ADD [importe_total] real NOT NULL DEFAULT CAST(0 AS real);

GO

ALTER TABLE [IG_GriesserSyncPresupuestos] ADD [importe_transporte] real NOT NULL DEFAULT CAST(0 AS real);

GO

ALTER TABLE [IG_GriesserSyncPresupuestos] ADD [superficie] real NULL;

GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20201223103312_CamposPresupuesto', N'3.1.9');

GO

DECLARE @var3 sysname;
SELECT @var3 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[IG_GriesserSyncPresupuestos]') AND [c].[name] = N'importe_transporte');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [IG_GriesserSyncPresupuestos] DROP CONSTRAINT [' + @var3 + '];');
ALTER TABLE [IG_GriesserSyncPresupuestos] ALTER COLUMN [importe_transporte] real NULL;

GO

DECLARE @var4 sysname;
SELECT @var4 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[IG_GriesserSyncPresupuestos]') AND [c].[name] = N'importe_tejido');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [IG_GriesserSyncPresupuestos] DROP CONSTRAINT [' + @var4 + '];');
ALTER TABLE [IG_GriesserSyncPresupuestos] ALTER COLUMN [importe_tejido] real NULL;

GO

DECLARE @var5 sysname;
SELECT @var5 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[IG_GriesserSyncPresupuestos]') AND [c].[name] = N'importe_tapas_y_testeros');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [IG_GriesserSyncPresupuestos] DROP CONSTRAINT [' + @var5 + '];');
ALTER TABLE [IG_GriesserSyncPresupuestos] ALTER COLUMN [importe_tapas_y_testeros] real NULL;

GO

DECLARE @var6 sysname;
SELECT @var6 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[IG_GriesserSyncPresupuestos]') AND [c].[name] = N'importe_incrementos');
IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [IG_GriesserSyncPresupuestos] DROP CONSTRAINT [' + @var6 + '];');
ALTER TABLE [IG_GriesserSyncPresupuestos] ALTER COLUMN [importe_incrementos] real NULL;

GO

DECLARE @var7 sysname;
SELECT @var7 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[IG_GriesserSyncPresupuestos]') AND [c].[name] = N'importe_color');
IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [IG_GriesserSyncPresupuestos] DROP CONSTRAINT [' + @var7 + '];');
ALTER TABLE [IG_GriesserSyncPresupuestos] ALTER COLUMN [importe_color] real NULL;

GO

DECLARE @var8 sysname;
SELECT @var8 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[IG_GriesserSyncPresupuestos]') AND [c].[name] = N'importe_automatismos');
IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [IG_GriesserSyncPresupuestos] DROP CONSTRAINT [' + @var8 + '];');
ALTER TABLE [IG_GriesserSyncPresupuestos] ALTER COLUMN [importe_automatismos] real NULL;

GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20201223103902_CamposNulos', N'3.1.9');

GO

DECLARE @var9 sysname;
SELECT @var9 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[IG_GriesserSyncPresupuestos]') AND [c].[name] = N'POS');
IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [IG_GriesserSyncPresupuestos] DROP CONSTRAINT [' + @var9 + '];');
ALTER TABLE [IG_GriesserSyncPresupuestos] ALTER COLUMN [POS] nvarchar(max) NULL;

GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20201223110853_PosToString', N'3.1.9');

GO

ALTER TABLE [IG_GriesserSyncPresupuestos] ADD [Cliente] nvarchar(max) NULL;

GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20201228143758_AddCliente', N'3.1.9');

GO

