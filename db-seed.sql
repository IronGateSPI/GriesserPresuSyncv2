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

Q