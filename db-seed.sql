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

