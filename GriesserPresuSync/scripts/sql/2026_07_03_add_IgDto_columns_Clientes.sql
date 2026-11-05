/* ============================================================================
   Migración: añadir columnas IgDtoWeinor e IgDtoMallorq a dbo.Clientes
   Motor     : SQL Server
   Fecha     : 2026-07-03
   Autor     : Albert

   Propósito : En producción estas columnas ya existen. Este script las crea
               en la copia local de desarrollo para que la query del worker
               (GriesserSyncClientesController.SqlConsultaCliente) no falle.

   Tipo elegido: mismo que [%Descuento] en Sage 200 Advanced España → DECIMAL(5,2)
                 Rango: -999.99 … 999.99 (suficiente para porcentajes de descuento).

   IMPORTANTE: Script IDEMPOTENTE — usa COL_LENGTH para no fallar si las
               columnas ya existen.
   ============================================================================ */

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH(N'dbo.Clientes', N'IgDtoWeinor') IS NULL
    BEGIN
        ALTER TABLE dbo.Clientes
            ADD IgDtoWeinor DECIMAL(5, 2) NULL
                CONSTRAINT DF_Clientes_IgDtoWeinor DEFAULT (0);
        PRINT N'[OK] Añadida columna dbo.Clientes.IgDtoWeinor';
    END
    ELSE
        PRINT N'[SKIP] La columna dbo.Clientes.IgDtoWeinor ya existe.';

    IF COL_LENGTH(N'dbo.Clientes', N'IgDtoMallorq') IS NULL
    BEGIN
        ALTER TABLE dbo.Clientes
            ADD IgDtoMallorq DECIMAL(5, 2) NULL
                CONSTRAINT DF_Clientes_IgDtoMallorq DEFAULT (0);
        PRINT N'[OK] Añadida columna dbo.Clientes.IgDtoMallorq';
    END
    ELSE
        PRINT N'[SKIP] La columna dbo.Clientes.IgDtoMallorq ya existe.';

    COMMIT TRANSACTION;
    PRINT N'Migración IgDto finalizada correctamente.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSev INT            = ERROR_SEVERITY();
    DECLARE @ErrSta INT            = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSev, @ErrSta);
END CATCH;
