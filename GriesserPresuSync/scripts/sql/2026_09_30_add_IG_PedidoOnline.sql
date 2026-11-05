/* ============================================================================
   Migración: campo IG_PedidoOnline (pedido online sí/no)
   Motor     : SQL Server
   Fecha     : 2026-09-30  (tipos verificados contra BDGRIESSER el 2026-10-01)
   Autor     : Albert

   Recorrido del campo:

     MiGriesser (JSON "pedido_online": 1 / null)
        -> IG_GriesserSyncPresupuestos.IG_PedidoOnline   (staging del servicio)
        -> CabeceraOfertaCliente.IG_PedidoOnline         (Sage)
        -> CabeceraPedidoCliente.IG_PedidoOnline         (Sage)
        -> CabeceraAlbaranCliente.IG_PedidoOnline        (Sage)
        -> ResumenCliente.IG_PedidoOnline                (Sage, cabecera factura)

   De CabeceraPedidoCliente lo lee el worker para el campo "pedidos" del PUT,
   y de ResumenCliente para "facturacion_desglosada".

   TIPOS — verificados sobre la base del cliente, NO son uniformes:

     Tablas de Sage      SMALLINT NOT NULL, convención -1 = sí / 0 = no
     Tablas de staging   INT NULL, misma convención de valores

   La diferencia es deliberada en origen (Sage usa smallint para sus campos
   sí/no) y el servicio la respeta: la entidad EF declara int? para el staging
   y el lector ADO.NET usa Convert.ToInt32, que acepta ambos tipos sin
   conversión explícita.

   Script IDEMPOTENTE: COL_LENGTH evita fallar si la columna ya existe.
   En la base del cliente ya están todas creadas; este script sirve para
   levantar entornos nuevos y como documentación del circuito.
   ============================================================================ */

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    -- ------------------------------------------------------------------
    -- Staging del servicio: INT NULL
    -- ------------------------------------------------------------------
    IF OBJECT_ID(N'dbo.IG_GriesserSyncPresupuestos', N'U') IS NOT NULL
       AND COL_LENGTH(N'dbo.IG_GriesserSyncPresupuestos', N'IG_PedidoOnline') IS NULL
    BEGIN
        ALTER TABLE dbo.IG_GriesserSyncPresupuestos ADD IG_PedidoOnline INT NULL;
        PRINT N'[OK] Añadida IG_GriesserSyncPresupuestos.IG_PedidoOnline';
    END
    ELSE
        PRINT N'[SKIP] IG_GriesserSyncPresupuestos.IG_PedidoOnline ya existe o la tabla no está.';

    IF OBJECT_ID(N'dbo.IG_GriesserSyncMallor', N'U') IS NOT NULL
       AND COL_LENGTH(N'dbo.IG_GriesserSyncMallor', N'IG_PedidoOnline') IS NULL
    BEGIN
        ALTER TABLE dbo.IG_GriesserSyncMallor ADD IG_PedidoOnline INT NULL;
        PRINT N'[OK] Añadida IG_GriesserSyncMallor.IG_PedidoOnline';
    END
    ELSE
        PRINT N'[SKIP] IG_GriesserSyncMallor.IG_PedidoOnline ya existe o la tabla no está.';

    -- ------------------------------------------------------------------
    -- Tablas de Sage: SMALLINT NOT NULL DEFAULT 0
    -- ------------------------------------------------------------------
    IF COL_LENGTH(N'dbo.CabeceraOfertaCliente', N'IG_PedidoOnline') IS NULL
    BEGIN
        ALTER TABLE dbo.CabeceraOfertaCliente
            ADD IG_PedidoOnline SMALLINT NOT NULL
                CONSTRAINT DF_CabOferta_IG_PedidoOnline DEFAULT (0);
        PRINT N'[OK] Añadida CabeceraOfertaCliente.IG_PedidoOnline';
    END
    ELSE
        PRINT N'[SKIP] CabeceraOfertaCliente.IG_PedidoOnline ya existe.';

    IF COL_LENGTH(N'dbo.CabeceraPedidoCliente', N'IG_PedidoOnline') IS NULL
    BEGIN
        ALTER TABLE dbo.CabeceraPedidoCliente
            ADD IG_PedidoOnline SMALLINT NOT NULL
                CONSTRAINT DF_CabPedido_IG_PedidoOnline DEFAULT (0);
        PRINT N'[OK] Añadida CabeceraPedidoCliente.IG_PedidoOnline';
    END
    ELSE
        PRINT N'[SKIP] CabeceraPedidoCliente.IG_PedidoOnline ya existe.';

    IF COL_LENGTH(N'dbo.CabeceraAlbaranCliente', N'IG_PedidoOnline') IS NULL
    BEGIN
        ALTER TABLE dbo.CabeceraAlbaranCliente
            ADD IG_PedidoOnline SMALLINT NOT NULL
                CONSTRAINT DF_CabAlbaran_IG_PedidoOnline DEFAULT (0);
        PRINT N'[OK] Añadida CabeceraAlbaranCliente.IG_PedidoOnline';
    END
    ELSE
        PRINT N'[SKIP] CabeceraAlbaranCliente.IG_PedidoOnline ya existe.';

    IF COL_LENGTH(N'dbo.ResumenCliente', N'IG_PedidoOnline') IS NULL
    BEGIN
        ALTER TABLE dbo.ResumenCliente
            ADD IG_PedidoOnline SMALLINT NOT NULL
                CONSTRAINT DF_ResumenCliente_IG_PedidoOnline DEFAULT (0);
        PRINT N'[OK] Añadida ResumenCliente.IG_PedidoOnline';
    END
    ELSE
        PRINT N'[SKIP] ResumenCliente.IG_PedidoOnline ya existe.';

    COMMIT TRANSACTION;
    PRINT N'Migración IG_PedidoOnline finalizada correctamente.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSev INT            = ERROR_SEVERITY();
    DECLARE @ErrSta INT            = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSev, @ErrSta);
END CATCH;
