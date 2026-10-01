IF SCHEMA_ID(N'planning') IS NULL
BEGIN
    EXEC(N'CREATE SCHEMA planning');
END;
GO

IF OBJECT_ID(N'planning.SmtLineFamilyRates', N'U') IS NULL
BEGIN
    CREATE TABLE planning.SmtLineFamilyRates
    (
        SmtLineFamilyRateId     int IDENTITY(1,1) NOT NULL,
        OperationId             int NOT NULL,
        ProductFamily           nvarchar(120) NOT NULL,
        LineCode                nvarchar(60) NOT NULL,
        LineNumber              int NOT NULL CONSTRAINT DF_SmtLineFamilyRates_LineNumber DEFAULT (0),
        UnitsPerHour            decimal(18,6) NOT NULL,
        SourcePartNumber        nvarchar(80) NULL,
        BottleneckUnitsPerHour  decimal(18,6) NULL,
        RouterUnitsPerHour      decimal(18,6) NULL,
        WaveUnitsPerHour        decimal(18,6) NULL,
        PressfitUnitsPerHour    decimal(18,6) NULL,
        IsActive                bit NOT NULL CONSTRAINT DF_SmtLineFamilyRates_IsActive DEFAULT (1),
        CONSTRAINT PK_SmtLineFamilyRates PRIMARY KEY CLUSTERED (SmtLineFamilyRateId),
        CONSTRAINT FK_SmtLineFamilyRates_Operations FOREIGN KEY (OperationId)
            REFERENCES planning.Operations (OperationId),
        CONSTRAINT UQ_SmtLineFamilyRates_Operation_Family_Line UNIQUE
            (OperationId, ProductFamily, LineCode),
        CONSTRAINT CK_SmtLineFamilyRates_UnitsPerHour CHECK (UnitsPerHour > 0)
    );
END;
GO

SELECT
    COUNT(*) AS SmtLineFamilyRateRows
FROM planning.SmtLineFamilyRates;
GO
