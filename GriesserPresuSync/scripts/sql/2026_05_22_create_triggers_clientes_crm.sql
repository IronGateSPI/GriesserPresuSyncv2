/* ============================================================================
   Migración: triggers para alimentar IG_CRM_ClientesPendientes
   Motor       : SQL Server
   Fecha       : 2026-05-22
   Autor       : Albert
   Descripción : Crea (o re-crea) los dos triggers que detectan cambios en
                 partners y los encolan para que el servicio Windows
                 GriesserPresuSync los envíe al CRM.

                 1) tr_Clientes_CRM_AfterIUD
                    Sobre dbo.Clientes (AFTER INSERT, UPDATE, DELETE).
                    Filtra: zzpartner = -1, CodigoCategoriaCliente_ = 'CLI',
                    CodigoEmpresa = 1.
                    Inserta operaciones I/U/D en la cola.

                 2) tr_ClientesImportesRiesgo_CRM_AfterIUD
                    Sobre dbo.ClientesImportesRiesgo (AFTER INSERT, UPDATE, DELETE).
                    Sólo encola si el cliente es partner (lookup en Clientes).

   Diseño robusto:
   - Trigger ligero: SÓLO un INSERT en la cola, sin acceso a red ni JOIN
     pesados. La consulta enriquecida (SELECT con outer apply) la hace el
     worker fuera del trigger.
   - Idempotente: si el trigger existe, se DROP y se CREA. Se puede ejecutar
     N veces sin riesgo.
   - SET NOCOUNT ON dentro del trigger para no romper aplicaciones que
     leen rowcounts (Sage).

   IMPORTANTE
   - Se asume que la tabla IG_CRM_ClientesPendientes existe (ejecuta antes
     2026_05_22_create_IG_CRM_ClientesPendientes.sql).
   ============================================================================ */

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    -- Pre-check: cola y tablas origen
    IF OBJECT_ID(N'dbo.IG_CRM_ClientesPendientes', N'U') IS NULL
    BEGIN
        RAISERROR(N'Falta dbo.IG_CRM_ClientesPendientes. Ejecuta antes 2026_05_22_create_IG_CRM_ClientesPendientes.sql.', 16, 1);
    END
    IF OBJECT_ID(N'dbo.Clientes', N'U') IS NULL
    BEGIN
        RAISERROR(N'No existe dbo.Clientes en esta base de datos. Aborto.', 16, 1);
    END
    IF OBJECT_ID(N'dbo.ClientesImportesRiesgo', N'U') IS NULL
    BEGIN
        RAISERROR(N'No existe dbo.ClientesImportesRiesgo en esta base de datos. Aborto.', 16, 1);
    END

    COMMIT TRANSACTION;
    PRINT N'Pre-checks OK. Procediendo a crear/recrear triggers.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSev INT            = ERROR_SEVERITY();
    DECLARE @ErrSta INT            = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSev, @ErrSta);
    RETURN;
END CATCH;

GO

-- ===========================================================================
-- TRIGGER 1: tr_Clientes_CRM_AfterIUD
-- ===========================================================================
IF OBJECT_ID(N'dbo.tr_Clientes_CRM_AfterIUD', N'TR') IS NOT NULL
BEGIN
    DROP TRIGGER dbo.tr_Clientes_CRM_AfterIUD;
    PRINT N'[OK] Trigger dbo.tr_Clientes_CRM_AfterIUD eliminado (se re-crea a continuación).';
END
GO

CREATE TRIGGER dbo.tr_Clientes_CRM_AfterIUD
ON dbo.Clientes
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    /* INSERT y UPDATE
       - I: existe en inserted pero no en deleted.
       - U: existe en ambos.
       Filtramos por partner (-1), categoría 'CLI' y empresa 1, igual que el
       SELECT que envía el worker, para no encolar clientes irrelevantes. */
    INSERT INTO dbo.IG_CRM_ClientesPendientes
        (CodigoEmpresa, CodigoCliente, Operacion, Origen, EstadoEnvio)
    SELECT
        i.CodigoEmpresa,
        i.CodigoCliente,
        CASE WHEN d.CodigoCliente IS NULL THEN 'I' ELSE 'U' END,
        'Clientes',
        'Pendiente'
    FROM inserted i
    LEFT JOIN deleted d
      ON  d.CodigoEmpresa = i.CodigoEmpresa
      AND d.CodigoCliente = i.CodigoCliente
    WHERE i.CodigoEmpresa            = 1
      AND i.CodigoCategoriaCliente_  = 'CLI'
      AND i.zzpartner                = -1;

    /* DELETE puro o UPDATE que despromociona (zzpartner -1 -> otro)
       Solo se encola para trazabilidad. El worker NO llama al CRM en
       operaciones 'D' (decisión funcional: el CRM no expone delete y se
       acordó dejarlo como histórico). El estado pasará a 'Descartado'
       cuando el worker lo procese. */
    INSERT INTO dbo.IG_CRM_ClientesPendientes
        (CodigoEmpresa, CodigoCliente, Operacion, Origen, EstadoEnvio)
    SELECT
        d.CodigoEmpresa,
        d.CodigoCliente,
        'D',
        'Clientes',
        'Pendiente'
    FROM deleted d
    LEFT JOIN inserted i
      ON  i.CodigoEmpresa = d.CodigoEmpresa
      AND i.CodigoCliente = d.CodigoCliente
    WHERE d.CodigoEmpresa           = 1
      AND d.CodigoCategoriaCliente_ = 'CLI'
      AND d.zzpartner               = -1
      AND (i.CodigoCliente IS NULL OR ISNULL(i.zzpartner, 0) <> -1);
END
GO

PRINT N'[OK] Trigger dbo.tr_Clientes_CRM_AfterIUD creado.';
GO


-- ===========================================================================
-- TRIGGER 2: tr_ClientesImportesRiesgo_CRM_AfterIUD
-- ===========================================================================
IF OBJECT_ID(N'dbo.tr_ClientesImportesRiesgo_CRM_AfterIUD', N'TR') IS NOT NULL
BEGIN
    DROP TRIGGER dbo.tr_ClientesImportesRiesgo_CRM_AfterIUD;
    PRINT N'[OK] Trigger dbo.tr_ClientesImportesRiesgo_CRM_AfterIUD eliminado (se re-crea a continuación).';
END
GO

CREATE TRIGGER dbo.tr_ClientesImportesRiesgo_CRM_AfterIUD
ON dbo.ClientesImportesRiesgo
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    /* Cambios en riesgo afectan a "descubierto" y "CyC descubierto".
       Tomamos los clientes afectados (inserted UNION deleted) y SÓLO
       encolamos los que son partner en Clientes. Operación siempre 'U'
       porque ResumenCliente/Riesgo no son la fuente de existencia del
       cliente; sólo de datos agregados. */
    ;WITH afectados AS
    (
        SELECT CodigoEmpresa, CodigoCliente FROM inserted
        UNION
        SELECT CodigoEmpresa, CodigoCliente FROM deleted
    )
    INSERT INTO dbo.IG_CRM_ClientesPendientes
        (CodigoEmpresa, CodigoCliente, Operacion, Origen, EstadoEnvio)
    SELECT
        a.CodigoEmpresa,
        a.CodigoCliente,
        'U',
        'ClientesImportesRiesgo',
        'Pendiente'
    FROM afectados a
    INNER JOIN dbo.Clientes c
      ON  c.CodigoEmpresa = a.CodigoEmpresa
      AND c.CodigoCliente = a.CodigoCliente
    WHERE c.CodigoEmpresa            = 1
      AND c.CodigoCategoriaCliente_  = 'CLI'
      AND c.zzpartner                = -1;
END
GO

PRINT N'[OK] Trigger dbo.tr_ClientesImportesRiesgo_CRM_AfterIUD creado.';
GO

PRINT N'Triggers CRM creados correctamente.';

/* ----------------------------------------------------------------------------
   ROLLBACK (manual, descomentar para revertir):

   IF OBJECT_ID(N'dbo.tr_Clientes_CRM_AfterIUD', N'TR') IS NOT NULL
       DROP TRIGGER dbo.tr_Clientes_CRM_AfterIUD;
   IF OBJECT_ID(N'dbo.tr_ClientesImportesRiesgo_CRM_AfterIUD', N'TR') IS NOT NULL
       DROP TRIGGER dbo.tr_ClientesImportesRiesgo_CRM_AfterIUD;
   ---------------------------------------------------------------------------- */
