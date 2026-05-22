/* ============================================================================
   Migración manual: añadir columnas altura_tapa y ancho_tapa
   Tabla destino : IG_GriesserSyncPresupuestos
   Motor         : SQL Server
   Fecha         : 2026-05-05
   Autor         : Albert
   Descripción   : Añade dos columnas DECIMAL(10,2) NULL para almacenar las
                   dimensiones de la tapa que devuelve la API
                   https://www.migriesser.com/es/budgets/api/ en cada línea.
                   Mapeo en código:
                     LineaPresupuesto.altura_tapa  ->  altura_tapa
                     LineaPresupuesto.ancho_tapa   ->  ancho_tapa

   IMPORTANTE
   - Script IDEMPOTENTE: usa COL_LENGTH para no fallar si la columna ya existe.
   - Las columnas son NULL para no romper filas históricas (legacy) ni el
     INSERT cuando la API no envíe el campo.
   - No se eliminan columnas, no se renombra nada: 100% aditivo y reversible
     (ver bloque ROLLBACK al final, comentado).
   ============================================================================ */

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    -- Sanity check: la tabla destino debe existir
    IF OBJECT_ID(N'dbo.IG_GriesserSyncPresupuestos', N'U') IS NULL
    BEGIN
        RAISERROR(N'La tabla dbo.IG_GriesserSyncPresupuestos no existe en esta base de datos. Aborto.', 16, 1);
    END

    -- altura_tapa
    IF COL_LENGTH(N'dbo.IG_GriesserSyncPresupuestos', N'altura_tapa') IS NULL
    BEGIN
        ALTER TABLE dbo.IG_GriesserSyncPresupuestos
            ADD altura_tapa DECIMAL(10, 2) NULL;
        PRINT N'[OK] Añadida columna altura_tapa DECIMAL(10,2) NULL';
    END
    ELSE
    BEGIN
        PRINT N'[SKIP] La columna altura_tapa ya existe; no se modifica.';
    END

    -- ancho_tapa
    IF COL_LENGTH(N'dbo.IG_GriesserSyncPresupuestos', N'ancho_tapa') IS NULL
    BEGIN
        ALTER TABLE dbo.IG_GriesserSyncPresupuestos
            ADD ancho_tapa DECIMAL(10, 2) NULL;
        PRINT N'[OK] Añadida columna ancho_tapa DECIMAL(10,2) NULL';
    END
    ELSE
    BEGIN
        PRINT N'[SKIP] La columna ancho_tapa ya existe; no se modifica.';
    END

    COMMIT TRANSACTION;
    PRINT N'Migración finalizada correctamente.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSev INT            = ERROR_SEVERITY();
    DECLARE @ErrSta INT            = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSev, @ErrSta);
END CATCH;

/* ----------------------------------------------------------------------------
   ROLLBACK (manual, descomentar solo si se quiere revertir).
   Cuidado: esto BORRA los datos almacenados en estas columnas.

   IF COL_LENGTH(N'dbo.IG_GriesserSyncPresupuestos', N'altura_tapa') IS NOT NULL
       ALTER TABLE dbo.IG_GriesserSyncPresupuestos DROP COLUMN altura_tapa;
   IF COL_LENGTH(N'dbo.IG_GriesserSyncPresupuestos', N'ancho_tapa') IS NOT NULL
       ALTER TABLE dbo.IG_GriesserSyncPresupuestos DROP COLUMN ancho_tapa;
   ---------------------------------------------------------------------------- */
