IF SCHEMA_ID(N'planning') IS NULL
BEGIN
    EXEC(N'CREATE SCHEMA planning');
END;
GO

IF OBJECT_ID(N'planning.SmtFamilies', N'U') IS NULL
BEGIN
    CREATE TABLE planning.SmtFamilies
    (
        SmtFamilyId     int IDENTITY(1,1) NOT NULL,
        ProductFamily   nvarchar(120) NOT NULL,
        BusinessUnit    nvarchar(80) NULL,
        Notes           nvarchar(400) NULL,
        IsActive        bit NOT NULL CONSTRAINT DF_SmtFamilies_IsActive DEFAULT (1),
        CreatedAtUtc    datetime2(0) NOT NULL CONSTRAINT DF_SmtFamilies_CreatedAtUtc DEFAULT (sysutcdatetime()),
        UpdatedAtUtc    datetime2(0) NOT NULL CONSTRAINT DF_SmtFamilies_UpdatedAtUtc DEFAULT (sysutcdatetime()),
        CONSTRAINT PK_SmtFamilies PRIMARY KEY CLUSTERED (SmtFamilyId),
        CONSTRAINT UQ_SmtFamilies_ProductFamily UNIQUE (ProductFamily)
    );
END;
GO

INSERT INTO planning.SmtFamilies (ProductFamily, BusinessUnit, Notes, IsActive)
SELECT
    families.ProductFamily,
    MAX(families.BusinessUnit) AS BusinessUnit,
    N'Familia detectada desde datos SMT existentes.' AS Notes,
    1 AS IsActive
FROM
(
    SELECT ProductFamily, BusinessUnit
    FROM planning.SmtFamilyVolumes
    WHERE ProductFamily IS NOT NULL

    UNION ALL

    SELECT ProductFamily, NULL
    FROM planning.SmtFamilyRates
    WHERE ProductFamily IS NOT NULL

    UNION ALL

    SELECT ProductFamily, NULL
    FROM planning.SmtLineFamilyRates
    WHERE ProductFamily IS NOT NULL

    UNION ALL

    SELECT ProductFamily, NULL
    FROM planning.SmtLineAssignments
    WHERE ProductFamily IS NOT NULL

    UNION ALL

    SELECT ProductFamily, BusinessUnit
    FROM planning.PartNumbers
    WHERE ProductFamily IS NOT NULL
) AS families
WHERE NOT EXISTS
(
    SELECT 1
    FROM planning.SmtFamilies existing
    WHERE existing.ProductFamily = families.ProductFamily
)
GROUP BY families.ProductFamily;
GO
