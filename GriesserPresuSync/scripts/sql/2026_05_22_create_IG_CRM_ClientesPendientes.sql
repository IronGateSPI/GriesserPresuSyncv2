/* ============================================================================
   Migración: crear tabla auxiliar IG_CRM_ClientesPendientes
   Tabla destino : IG_CRM_ClientesPendientes
   Motor         : SQL Server
   Fecha         : 2026-05-22
   Autor         : Albert
   Descripción   : Tabla "cola" donde los triggers de Sage (Clientes,
                   ClientesImportesRiesgo) registran qué clientes han
                   cambiado. El servicio Windows GriesserPresuSync lee
                   esta tabla cada 30 s (configurable), consulta los
                   datos consolidados del cliente y los envía por PUT
                   al CRM https://www.crmgriesser.es/

   IMPORTANTE
   - Script IDEMPOTENTE: usa OBJECT_ID/COL_LENGTH para no fallar si la
     tabla o columnas ya existen.
   - No se eliminan columnas, no se renombra nada: 100% aditivo.
   - Bloque ROLLBACK comentado al final por si hace falta revertir.
   ============================================================================ */

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    -- ------------------------------------------------------------------
    -- 1) Crear la tabla si no existe
    -- ------------------------------------------------------------------
    IF OBJECT_ID(N'dbo.IG_CRM_ClientesPendientes', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.IG_CRM_ClientesPendientes
        (
            Id              BIGINT          IDENTITY(1,1) NOT NULL,
            CodigoEmpresa   SMALLINT        NOT NULL,
            CodigoCliente   VARCHAR(15)     NOT NULL,
            Operacion       CHAR(1)         NOT NULL,
            Origen          VARCHAR(40)     NOT NULL,
            FechaCambio     DATETIME        NOT NULL CONSTRAINT DF_IG_CRM_Pend_FechaCambio DEFAULT (GETDATE()),
            FechaProcesado  DATETIME        NULL,
            EstadoEnvio     VARCHAR(20)     NOT NULL CONSTRAINT DF_IG_CRM_Pend_Estado DEFAULT ('Pendiente'),
            Intentos        INT             NOT NULL CONSTRAINT DF_IG_CRM_Pend_Intentos DEFAULT (0),
            UltimoError     NVARCHAR(2000)  NULL,
            PayloadEnviado  NVARCHAR(MAX)   NULL,
            CONSTRAINT PK_IG_CRM_ClientesPendientes PRIMARY KEY CLUSTERED (Id)
        );
        PRINT N'[OK] Creada tabla dbo.IG_CRM_ClientesPendientes';
    END
    ELSE
    BEGIN
        PRINT N'[SKIP] La tabla dbo.IG_CRM_ClientesPendientes ya existe.';
    END

    -- ------------------------------------------------------------------
    -- 2) Asegurar las columnas (por si la tabla existía con menos campos)
    --    Cada bloque es independiente y solo añade lo que falte.
    -- ------------------------------------------------------------------
    IF COL_LENGTH(N'dbo.IG_CRM_ClientesPendientes', N'CodigoEmpresa') IS NULL
    BEGIN
        ALTER TABLE dbo.IG_CRM_ClientesPendientes ADD CodigoEmpresa SMALLINT NOT NULL DEFAULT (1);
        PRINT N'[OK] Añadida columna CodigoEmpresa';
    END

    IF COL_LENGTH(N'dbo.IG_CRM_ClientesPendientes', N'CodigoCliente') IS NULL
    BEGIN
        ALTER TABLE dbo.IG_CRM_ClientesPendientes ADD CodigoCliente VARCHAR(15) NOT NULL DEFAULT ('');
        PRINT N'[OK] Añadida columna CodigoCliente';
    END

    IF COL_LENGTH(N'dbo.IG_CRM_ClientesPendientes', N'Operacion') IS NULL
    BEGIN
        ALTER TABLE dbo.IG_CRM_ClientesPendientes ADD Operacion CHAR(1) NOT NULL DEFAULT ('U');
        PRINT N'[OK] Añadida columna Operacion';
    END

    IF COL_LENGTH(N'dbo.IG_CRM_ClientesPendientes', N'Origen') IS NULL
    BEGIN
        ALTER TABLE dbo.IG_CRM_ClientesPendientes ADD Origen VARCHAR(40) NOT NULL DEFAULT ('Clientes');
        PRINT N'[OK] Añadida columna Origen';
    END

    IF COL_LENGTH(N'dbo.IG_CRM_ClientesPendientes', N'FechaCambio') IS NULL
    BEGIN
        ALTER TABLE dbo.IG_CRM_ClientesPendientes ADD FechaCambio DATETIME NOT NULL DEFAULT (GETDATE());
        PRINT N'[OK] Añadida columna FechaCambio';
    END

    IF COL_LENGTH(N'dbo.IG_CRM_ClientesPendientes', N'FechaProcesado') IS NULL
    BEGIN
        ALTER TABLE dbo.IG_CRM_ClientesPendientes ADD FechaProcesado DATETIME NULL;
        PRINT N'[OK] Añadida columna FechaProcesado';
    END

    IF COL_LENGTH(N'dbo.IG_CRM_ClientesPendientes', N'EstadoEnvio') IS NULL
    BEGIN
        ALTER TABLE dbo.IG_CRM_ClientesPendientes ADD EstadoEnvio VARCHAR(20) NOT NULL DEFAULT ('Pendiente');
        PRINT N'[OK] Añadida columna EstadoEnvio';
    END

    IF COL_LENGTH(N'dbo.IG_CRM_ClientesPendientes', N'Intentos') IS NULL
    BEGIN
        ALTER TABLE dbo.IG_CRM_ClientesPendientes ADD Intentos INT NOT NULL DEFAULT (0);
        PRINT N'[OK] Añadida columna Intentos';
    END

    IF COL_LENGTH(N'dbo.IG_CRM_ClientesPendientes', N'UltimoError') IS NULL
    BEGIN
        ALTER TABLE dbo.IG_CRM_ClientesPendientes ADD UltimoError NVARCHAR(2000) NULL;
        PRINT N'[OK] Añadida columna UltimoError';
    END

    IF COL_LENGTH(N'dbo.IG_CRM_ClientesPendientes', N'PayloadEnviado') IS NULL
    BEGIN
        ALTER TABLE dbo.IG_CRM_ClientesPendientes ADD PayloadEnviado NVARCHAR(MAX) NULL;
        PRINT N'[OK] Añadida columna PayloadEnviado';
    END

    -- ------------------------------------------------------------------
    -- 3) Índices: uno para el barrido del worker, otro para búsquedas
    --    rápidas por cliente (colapso de duplicados).
    -- ------------------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE name = N'IX_IG_CRM_Pend_Estado_Fecha'
                     AND object_id = OBJECT_ID(N'dbo.IG_CRM_ClientesPendientes'))
    BEGIN
        CREATE INDEX IX_IG_CRM_Pend_Estado_Fecha
            ON dbo.IG_CRM_ClientesPendientes (EstadoEnvio, FechaCambio);
        PRINT N'[OK] Creado índice IX_IG_CRM_Pend_Estado_Fecha';
    END
    ELSE
    BEGIN
        PRINT N'[SKIP] El índice IX_IG_CRM_Pend_Estado_Fecha ya existe.';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE name = N'IX_IG_CRM_Pend_Cliente'
                     AND object_id = OBJECT_ID(N'dbo.IG_CRM_ClientesPendientes'))
    BEGIN
        CREATE INDEX IX_IG_CRM_Pend_Cliente
            ON dbo.IG_CRM_ClientesPendientes (CodigoCliente);
        PRINT N'[OK] Creado índice IX_IG_CRM_Pend_Cliente';
    END
    ELSE
    BEGIN
        PRINT N'[SKIP] El índice IX_IG_CRM_Pend_Cliente ya existe.';
    END

    COMMIT TRANSACTION;
    PRINT N'Migración IG_CRM_ClientesPendientes finalizada correctamente.';
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
   CUIDADO: esto BORRA la cola completa y se pierde el histórico de envíos.

   IF OBJECT_ID(N'dbo.IG_CRM_ClientesPendientes', N'U') IS NOT NULL
       DROP TABLE dbo.IG_CRM_ClientesPendientes;
   ---------------------------------------------------------------------------- */
