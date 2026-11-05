/* ============================================================================
   Migración: deduplicar ráfagas en tr_ClientesImportesRiesgo_CRM_AfterIUD
   Motor     : SQL Server
   Fecha     : 2026-09-28
   Autor     : Albert

   PROBLEMA
   Al recalcular el riesgo de un cliente, Sage ejecuta muchas sentencias
   separadas contra ClientesImportesRiesgo. El trigger es a nivel de
   SENTENCIA, así que cada una encolaba una fila: se han medido hasta 26
   filas para el mismo cliente en un solo minuto.

   El worker ya colapsa esas filas en un único PUT, así que el CRM no recibe
   datos duplicados. El coste real es de almacenamiento: cada fila guardaba
   una copia completa del PayloadEnviado.

   SOLUCIÓN
   No encolar si ese cliente ya tiene una fila en estado 'Pendiente'.

   SIN ventana temporal, y sin riesgo de perder cambios, porque el worker
   RECLAMA las filas (las pasa a 'Procesando') ANTES de consultar Sage:

     - Mientras el worker procesa, la cola no tiene 'Pendiente' para ese
       cliente, así que un cambio que llegue en ese momento SÍ encola una
       fila nueva y el siguiente ciclo la recoge leyendo Sage otra vez.
     - La cola solo guarda el código de cliente, nunca los datos. El JSON se
       construye en el momento del envío, así que basta con que exista una
       marca pendiente para que el CRM reciba el estado actual.
     - Y el PUT envía siempre el estado completo, no incrementos.

   Requiere la versión del worker que reclama filas como 'Procesando'.
   Si se despliega este trigger con un worker antiguo, los cambios que
   lleguen mientras el worker envía se perderían hasta el siguiente evento.

   Script IDEMPOTENTE: re-crea el trigger; se puede ejecutar N veces.
   ============================================================================ */

SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.IG_CRM_ClientesPendientes', N'U') IS NULL
BEGIN
    RAISERROR(N'Falta dbo.IG_CRM_ClientesPendientes. Ejecuta antes el script de creación de la cola.', 16, 1);
END
GO

IF OBJECT_ID(N'dbo.tr_ClientesImportesRiesgo_CRM_AfterIUD', N'TR') IS NOT NULL
BEGIN
    DROP TRIGGER dbo.tr_ClientesImportesRiesgo_CRM_AfterIUD;
    PRINT N'[OK] Trigger anterior eliminado.';
END
GO

CREATE TRIGGER dbo.tr_ClientesImportesRiesgo_CRM_AfterIUD
ON dbo.ClientesImportesRiesgo
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

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
      AND c.zzpartner                = -1
      -- Colapsa la ráfaga: si ya hay una marca pendiente para este cliente,
      -- no hace falta otra. El worker la recogerá y leerá Sage al vuelo.
      AND NOT EXISTS (
            SELECT 1
            FROM dbo.IG_CRM_ClientesPendientes p WITH (NOLOCK)
            WHERE p.CodigoEmpresa = a.CodigoEmpresa
              AND p.CodigoCliente = a.CodigoCliente
              AND p.EstadoEnvio   = 'Pendiente'
      );
END
GO

PRINT N'[OK] Trigger tr_ClientesImportesRiesgo_CRM_AfterIUD re-creado con deduplicación.';
GO

/* Índice de apoyo para que el NOT EXISTS no penalice: el trigger corre dentro
   de la transacción de Sage, así que esta consulta debe ser inmediata. */
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = N'IX_IG_CRM_Pend_Dedupe'
                 AND object_id = OBJECT_ID(N'dbo.IG_CRM_ClientesPendientes'))
BEGIN
    CREATE INDEX IX_IG_CRM_Pend_Dedupe
        ON dbo.IG_CRM_ClientesPendientes (CodigoEmpresa, CodigoCliente, EstadoEnvio);
    PRINT N'[OK] Creado índice IX_IG_CRM_Pend_Dedupe.';
END
ELSE
    PRINT N'[SKIP] El índice IX_IG_CRM_Pend_Dedupe ya existe.';
GO

/* ----------------------------------------------------------------------------
   ROLLBACK: re-ejecutar 2026_05_22_create_triggers_clientes_crm.sql, que
   vuelve a crear la versión sin deduplicación.
   ---------------------------------------------------------------------------- */
