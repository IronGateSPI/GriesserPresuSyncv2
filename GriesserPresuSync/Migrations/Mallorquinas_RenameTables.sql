/* ============================================================================
   Migración manual de tablas de Mallorquinas
   ----------------------------------------------------------------------------
   Renombra las tablas (y FKs) creadas por la antigua migración
   AddMallorquinasTables (IG_GriesserSyncMallor / IG_GriesserSyncMallor2 ...)
   a los nombres definitivos:
     IG_GriesserSyncMallorquinas
     IG_GriesserSyncMallorquinas_Lineas
     IG_GriesserSyncMallorquinas_Mandos

   Idempotente: comprueba existencia antes de actuar.
   Si las tablas viejas no existen, las crea desde cero con la estructura
   nueva (incluyendo la columna IsSincronized en la cabecera).
   ============================================================================ */

SET NOCOUNT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    -- 1) RENAMES (si existen las tablas viejas) -----------------------------------------------
    IF OBJECT_ID('dbo.IG_GriesserSyncMallor2_Mandos', 'U') IS NOT NULL
        EXEC sp_rename 'dbo.IG_GriesserSyncMallor2_Mandos', 'IG_GriesserSyncMallorquinas_Mandos';

    IF OBJECT_ID('dbo.IG_GriesserSyncMallor2_Lineas', 'U') IS NOT NULL
        EXEC sp_rename 'dbo.IG_GriesserSyncMallor2_Lineas', 'IG_GriesserSyncMallorquinas_Lineas';

    IF OBJECT_ID('dbo.IG_GriesserSyncMallor2', 'U') IS NOT NULL
        EXEC sp_rename 'dbo.IG_GriesserSyncMallor2', 'IG_GriesserSyncMallorquinas';

    -- También cubrimos el caso en que algún proceso antiguo creara IG_GriesserSyncMallor (sin el "2")
    IF OBJECT_ID('dbo.IG_GriesserSyncMallor_Mandos', 'U') IS NOT NULL AND OBJECT_ID('dbo.IG_GriesserSyncMallorquinas_Mandos','U') IS NULL
        EXEC sp_rename 'dbo.IG_GriesserSyncMallor_Mandos', 'IG_GriesserSyncMallorquinas_Mandos';

    IF OBJECT_ID('dbo.IG_GriesserSyncMallor_Lineas', 'U') IS NOT NULL AND OBJECT_ID('dbo.IG_GriesserSyncMallorquinas_Lineas','U') IS NULL
        EXEC sp_rename 'dbo.IG_GriesserSyncMallor_Lineas', 'IG_GriesserSyncMallorquinas_Lineas';

    IF OBJECT_ID('dbo.IG_GriesserSyncMallor', 'U') IS NOT NULL AND OBJECT_ID('dbo.IG_GriesserSyncMallorquinas','U') IS NULL
        EXEC sp_rename 'dbo.IG_GriesserSyncMallor', 'IG_GriesserSyncMallorquinas';

    -- 2) CREACIÓN DESDE CERO (si no existían) -------------------------------------------------
    IF OBJECT_ID('dbo.IG_GriesserSyncMallorquinas', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.IG_GriesserSyncMallorquinas (
            IdLinea                    NVARCHAR(400)  NOT NULL,
            id_budget                  INT            NOT NULL,
            cod_client                 NVARCHAR(MAX)  NULL,
            date_created               DATETIME2      NULL,
            presupuesto                NVARCHAR(MAX)  NULL,
            expiration_date            DATETIME2      NULL,
            id_mallorquina_color       INT            NULL,
            cod_mallorquina_color      NVARCHAR(MAX)  NULL,
            id_mallorquina_acabado     INT            NULL,
            cod_mallorquina_acabado    NVARCHAR(MAX)  NULL,
            num_lines                  INT            NULL,
            price_lines                DECIMAL(18,2)  NULL,
            num_incrementos            INT            NULL,
            price_incrementos          DECIMAL(18,2)  NULL,
            price_color                DECIMAL(18,2)  NULL,
            importe_transporte         DECIMAL(18,2)  NULL,
            importe_instalacion        DECIMAL(18,2)  NULL,
            total                      DECIMAL(18,2)  NULL,
            IsSincronized              BIT            NOT NULL CONSTRAINT DF_IGMall_IsSinc DEFAULT(0),
            CONSTRAINT PK_IG_GriesserSyncMallorquinas PRIMARY KEY (IdLinea)
        );
    END
    ELSE
    BEGIN
        -- Asegurar que IsSincronized existe tras el rename (la tabla vieja no la tenía)
        IF COL_LENGTH('dbo.IG_GriesserSyncMallorquinas', 'IsSincronized') IS NULL
        BEGIN
            ALTER TABLE dbo.IG_GriesserSyncMallorquinas
                ADD IsSincronized BIT NOT NULL CONSTRAINT DF_IGMall_IsSinc DEFAULT(0);
        END
    END

    IF OBJECT_ID('dbo.IG_GriesserSyncMallorquinas_Lineas', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.IG_GriesserSyncMallorquinas_Lineas (
            IdLineaPadre                              NVARCHAR(400) NOT NULL,
            IdLinea                                   INT           NOT NULL,
            pos                                       NVARCHAR(50)  NULL,
            id_mallorquina_tipo                       INT           NULL,
            cod_mallorquina_tipo                      NVARCHAR(MAX) NULL,
            id_mallorquina_modelo                     INT           NULL,
            cod_mallorquina_modelo                    NVARCHAR(MAX) NULL,
            ancho_hueco                               INT           NULL,
            alto_hueco                                INT           NULL,
            id_mallorquina_esquema                    INT           NULL,
            cod_mallorquina_esquema                   NVARCHAR(MAX) NULL,
            id_mallorquina_tipologia_instalacion      INT           NULL,
            cod_mallorquina_tipologia_instalacion     NVARCHAR(MAX) NULL,
            motorizado                                BIT           NULL,
            ancho_hoja                                INT           NULL,
            alto_hoja                                 INT           NULL,
            precio_por_hoja                           DECIMAL(18,2) NULL,
            num_hojas                                 INT           NULL,
            precio_herraje                            DECIMAL(18,2) NULL,
            precio_incrementos                        DECIMAL(18,2) NULL,
            precio_automatismos                       DECIMAL(18,2) NULL,
            units                                     INT           NULL,
            total                                     DECIMAL(18,2) NULL,
            id_automatismo_marca                      INT           NULL,
            cod_automatismo_marca                     NVARCHAR(MAX) NULL,
            id_automatismo_receptor                   INT           NULL,
            cod_automatismo_receptor                  NVARCHAR(MAX) NULL,
            precio_receptor                           DECIMAL(18,2) NULL,
            precio_mandos                             DECIMAL(18,2) NULL,
            CONSTRAINT PK_IG_GriesserSyncMallorquinas_Lineas PRIMARY KEY (IdLineaPadre, IdLinea),
            CONSTRAINT FK_IGMall_Lineas_Cab FOREIGN KEY (IdLineaPadre)
                REFERENCES dbo.IG_GriesserSyncMallorquinas(IdLinea) ON DELETE CASCADE
        );
    END

    IF OBJECT_ID('dbo.IG_GriesserSyncMallorquinas_Mandos', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.IG_GriesserSyncMallorquinas_Mandos (
            IdLineaPadre              NVARCHAR(400)  NOT NULL,
            IdLinea                   INT            NOT NULL,
            id_automatismo_mando      INT            NOT NULL,
            cod_automatismo_mando     NVARCHAR(MAX)  NULL,
            price                     DECIMAL(18,2)  NULL,
            units                     INT            NULL,
            total                     DECIMAL(18,2)  NULL,
            CONSTRAINT PK_IG_GriesserSyncMallorquinas_Mandos PRIMARY KEY (IdLineaPadre, IdLinea, id_automatismo_mando),
            CONSTRAINT FK_IGMall_Mandos_Lin FOREIGN KEY (IdLineaPadre, IdLinea)
                REFERENCES dbo.IG_GriesserSyncMallorquinas_Lineas(IdLineaPadre, IdLinea) ON DELETE CASCADE
        );
    END

    COMMIT TRANSACTION;
    PRINT 'Migración de tablas Mallorquinas completada correctamente.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    DECLARE @msg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR('Error en la migración: %s', 16, 1, @msg);
END CATCH;
GO
