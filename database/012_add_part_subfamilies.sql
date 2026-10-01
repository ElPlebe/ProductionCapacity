/*
    ProductionCapacity - Catalogo de subfamilias para numeros de parte SMT

    Uso:
    1. Ejecuta dentro de la base ProductionCapacity.
    2. Este script es idempotente: puede ejecutarse mas de una vez.
*/

SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'planning.SubFamilies', N'U') IS NULL
BEGIN
    CREATE TABLE planning.SubFamilies
    (
        SubFamilyId     int IDENTITY(1,1) NOT NULL,
        ProductFamily   nvarchar(120) NOT NULL,
        SubFamilyName   nvarchar(120) NOT NULL,
        Notes           nvarchar(240) NULL,
        IsActive        bit NOT NULL CONSTRAINT DF_SubFamilies_IsActive DEFAULT (1),
        CreatedAtUtc    datetime2(0) NOT NULL CONSTRAINT DF_SubFamilies_CreatedAtUtc DEFAULT (sysutcdatetime()),
        UpdatedAtUtc    datetime2(0) NOT NULL CONSTRAINT DF_SubFamilies_UpdatedAtUtc DEFAULT (sysutcdatetime()),
        CONSTRAINT PK_SubFamilies PRIMARY KEY CLUSTERED (SubFamilyId),
        CONSTRAINT UQ_SubFamilies_ProductFamily_SubFamilyName UNIQUE (ProductFamily, SubFamilyName)
    );
END;
GO

IF COL_LENGTH(N'planning.PartNumbers', N'SubFamily') IS NULL
BEGIN
    ALTER TABLE planning.PartNumbers
        ADD SubFamily nvarchar(120) NULL;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'planning.PartNumbers')
        AND name = N'IX_PartNumbers_ProductFamily_SubFamily'
)
BEGIN
    CREATE INDEX IX_PartNumbers_ProductFamily_SubFamily
        ON planning.PartNumbers(ProductFamily, SubFamily);
END;
GO

SELECT
    N'012_add_part_subfamilies.sql' AS ScriptName,
    N'Subfamilias SMT listas' AS Resultado;
GO
