# GriesserPresuSync

## Descripción del proyecto

Es un Worker netcore que se puede ejecutar como servicio. Recupera de la api de migriesser.com los presupuestos y los inserta dentro de una tabla intermedia de SAGE.

## Como crear un ejecutable del servicio

Para poder generar un fichero EXE del proyecto ejecutar:

```
dotnet publish --self-contained --configuration Release --runtime win-x64 /p:PublishSingleFile=true
```

## Cambios en la BBDD
Añadir la migración con `Add-Migration <MigrationName>`

### Generar seed SQL
```
Script-Migration
```

### Changelog & SQL Migrations

* 1.0.2

```
DECLARE @var10 sysname;
SELECT @var10 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[IG_GriesserSyncPresupuestos]') AND [c].[name] = N'HL');
IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [IG_GriesserSyncPresupuestos] DROP CONSTRAINT [' + @var10 + '];');
ALTER TABLE [IG_GriesserSyncPresupuestos] ALTER COLUMN [HL] real NULL;

GO

ALTER TABLE [IG_GriesserSyncPresupuestos] ADD [GH] real NULL;

GO

ALTER TABLE [IG_GriesserSyncPresupuestos] ADD [accionamiento] nvarchar(max) NULL;

GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20210203112632_AccionamientoGH', N'3.1.9');

GO
```