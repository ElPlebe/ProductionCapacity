/*
    ProductionCapacity - Rack/cart physical capacity constraints

    Run this script inside the existing ProductionCapacity database after 001_create_planning_schema.sql.
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'planning.RackTypes', N'U') IS NULL
BEGIN
    CREATE TABLE planning.RackTypes
    (
        RackTypeId             int IDENTITY(1,1) NOT NULL,
        RackCode               nvarchar(60) NOT NULL,
        RackName               nvarchar(160) NOT NULL,
        BaseUnitsPerRack       decimal(18,4) NOT NULL,
        IsActive               bit NOT NULL CONSTRAINT DF_RackTypes_IsActive DEFAULT (1),
        CreatedAtUtc           datetime2(0) NOT NULL CONSTRAINT DF_RackTypes_CreatedAtUtc DEFAULT (sysutcdatetime()),
        UpdatedAtUtc           datetime2(0) NOT NULL CONSTRAINT DF_RackTypes_UpdatedAtUtc DEFAULT (sysutcdatetime()),
        CONSTRAINT PK_RackTypes PRIMARY KEY CLUSTERED (RackTypeId),
        CONSTRAINT UQ_RackTypes_RackCode UNIQUE (RackCode),
        CONSTRAINT CK_RackTypes_BaseUnitsPerRack CHECK (BaseUnitsPerRack > 0)
    );
END;
GO

IF OBJECT_ID(N'planning.RackAccessories', N'U') IS NULL
BEGIN
    CREATE TABLE planning.RackAccessories
    (
        RackAccessoryId            int IDENTITY(1,1) NOT NULL,
        AccessoryCode              nvarchar(60) NOT NULL,
        AccessoryName              nvarchar(160) NOT NULL,
        AdditionalUnitsPerRack     decimal(18,4) NOT NULL,
        IsActive                   bit NOT NULL CONSTRAINT DF_RackAccessories_IsActive DEFAULT (1),
        CreatedAtUtc               datetime2(0) NOT NULL CONSTRAINT DF_RackAccessories_CreatedAtUtc DEFAULT (sysutcdatetime()),
        UpdatedAtUtc               datetime2(0) NOT NULL CONSTRAINT DF_RackAccessories_UpdatedAtUtc DEFAULT (sysutcdatetime()),
        CONSTRAINT PK_RackAccessories PRIMARY KEY CLUSTERED (RackAccessoryId),
        CONSTRAINT UQ_RackAccessories_AccessoryCode UNIQUE (AccessoryCode),
        CONSTRAINT CK_RackAccessories_AdditionalUnitsPerRack CHECK (AdditionalUnitsPerRack >= 0)
    );
END;
GO

IF OBJECT_ID(N'planning.RackConfigurations', N'U') IS NULL
BEGIN
    CREATE TABLE planning.RackConfigurations
    (
        RackConfigurationId    int IDENTITY(1,1) NOT NULL,
        RackTypeId             int NOT NULL,
        RackAccessoryId        int NULL,
        MachineId              int NOT NULL,
        PartNumberId           int NOT NULL,
        UnitsPerRack           decimal(18,4) NOT NULL,
        Notes                  nvarchar(400) NULL,
        IsActive               bit NOT NULL CONSTRAINT DF_RackConfigurations_IsActive DEFAULT (1),
        CreatedAtUtc           datetime2(0) NOT NULL CONSTRAINT DF_RackConfigurations_CreatedAtUtc DEFAULT (sysutcdatetime()),
        UpdatedAtUtc           datetime2(0) NOT NULL CONSTRAINT DF_RackConfigurations_UpdatedAtUtc DEFAULT (sysutcdatetime()),
        CONSTRAINT PK_RackConfigurations PRIMARY KEY CLUSTERED (RackConfigurationId),
        CONSTRAINT FK_RackConfigurations_RackTypes FOREIGN KEY (RackTypeId)
            REFERENCES planning.RackTypes (RackTypeId),
        CONSTRAINT FK_RackConfigurations_RackAccessories FOREIGN KEY (RackAccessoryId)
            REFERENCES planning.RackAccessories (RackAccessoryId),
        CONSTRAINT FK_RackConfigurations_Machines FOREIGN KEY (MachineId)
            REFERENCES planning.Machines (MachineId),
        CONSTRAINT FK_RackConfigurations_PartNumbers FOREIGN KEY (PartNumberId)
            REFERENCES planning.PartNumbers (PartNumberId),
        CONSTRAINT UQ_RackConfigurations_Rack_Machine_Part_Accessory UNIQUE (RackTypeId, MachineId, PartNumberId, RackAccessoryId),
        CONSTRAINT CK_RackConfigurations_UnitsPerRack CHECK (UnitsPerRack > 0)
    );
END;
GO

IF OBJECT_ID(N'planning.ScenarioRackInventories', N'U') IS NULL
BEGIN
    CREATE TABLE planning.ScenarioRackInventories
    (
        ScenarioRackInventoryId    int IDENTITY(1,1) NOT NULL,
        ScenarioId                 int NOT NULL,
        ProductionWeekId           int NOT NULL,
        MachineId                  int NOT NULL,
        RackTypeId                 int NOT NULL,
        AvailableRackCount         int NOT NULL,
        Notes                      nvarchar(400) NULL,
        CreatedAtUtc               datetime2(0) NOT NULL CONSTRAINT DF_ScenarioRackInventories_CreatedAtUtc DEFAULT (sysutcdatetime()),
        UpdatedAtUtc               datetime2(0) NOT NULL CONSTRAINT DF_ScenarioRackInventories_UpdatedAtUtc DEFAULT (sysutcdatetime()),
        CONSTRAINT PK_ScenarioRackInventories PRIMARY KEY CLUSTERED (ScenarioRackInventoryId),
        CONSTRAINT FK_ScenarioRackInventories_Scenarios FOREIGN KEY (ScenarioId)
            REFERENCES planning.Scenarios (ScenarioId),
        CONSTRAINT FK_ScenarioRackInventories_ProductionWeeks FOREIGN KEY (ProductionWeekId)
            REFERENCES planning.ProductionWeeks (ProductionWeekId),
        CONSTRAINT FK_ScenarioRackInventories_Machines FOREIGN KEY (MachineId)
            REFERENCES planning.Machines (MachineId),
        CONSTRAINT FK_ScenarioRackInventories_RackTypes FOREIGN KEY (RackTypeId)
            REFERENCES planning.RackTypes (RackTypeId),
        CONSTRAINT UQ_ScenarioRackInventories_Scenario_Week_Machine_Rack UNIQUE (ScenarioId, ProductionWeekId, MachineId, RackTypeId),
        CONSTRAINT CK_ScenarioRackInventories_AvailableRackCount CHECK (AvailableRackCount >= 0)
    );
END;
GO

IF COL_LENGTH(N'planning.CapacityResults', N'TimeCapacityUnits') IS NULL
BEGIN
    ALTER TABLE planning.CapacityResults
        ADD TimeCapacityUnits decimal(18,4) NOT NULL CONSTRAINT DF_CapacityResults_TimeCapacityUnits DEFAULT (0),
            RackTypeId int NULL,
            RackConfigurationId int NULL,
            UnitsPerRack decimal(18,4) NULL,
            RequiredRackCount int NULL,
            AvailableRackCount int NULL,
            MissingRackCount int NULL,
            RackCapacityUnits decimal(18,4) NULL,
            IsRackConstrained bit NOT NULL CONSTRAINT DF_CapacityResults_IsRackConstrained DEFAULT (0);

    ALTER TABLE planning.CapacityResults
        ADD CONSTRAINT FK_CapacityResults_RackTypes FOREIGN KEY (RackTypeId)
            REFERENCES planning.RackTypes (RackTypeId),
            CONSTRAINT FK_CapacityResults_RackConfigurations FOREIGN KEY (RackConfigurationId)
            REFERENCES planning.RackConfigurations (RackConfigurationId),
            CONSTRAINT CK_CapacityResults_UnitsPerRack CHECK (UnitsPerRack IS NULL OR UnitsPerRack > 0),
            CONSTRAINT CK_CapacityResults_RequiredRackCount CHECK (RequiredRackCount IS NULL OR RequiredRackCount >= 0),
            CONSTRAINT CK_CapacityResults_AvailableRackCount CHECK (AvailableRackCount IS NULL OR AvailableRackCount >= 0),
            CONSTRAINT CK_CapacityResults_MissingRackCount CHECK (MissingRackCount IS NULL OR MissingRackCount >= 0),
            CONSTRAINT CK_CapacityResults_RackCapacityUnits CHECK (RackCapacityUnits IS NULL OR RackCapacityUnits >= 0);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RackConfigurations_Machine_Part' AND object_id = OBJECT_ID(N'planning.RackConfigurations'))
BEGIN
    CREATE INDEX IX_RackConfigurations_Machine_Part
        ON planning.RackConfigurations (MachineId, PartNumberId)
        INCLUDE (RackTypeId, RackAccessoryId, UnitsPerRack, IsActive);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ScenarioRackInventories_Scenario_Week_Machine' AND object_id = OBJECT_ID(N'planning.ScenarioRackInventories'))
BEGIN
    CREATE INDEX IX_ScenarioRackInventories_Scenario_Week_Machine
        ON planning.ScenarioRackInventories (ScenarioId, ProductionWeekId, MachineId)
        INCLUDE (RackTypeId, AvailableRackCount);
END;
GO

CREATE OR ALTER VIEW planning.vwLatestCapacityResults
AS
WITH LatestRun AS
(
    SELECT
        cr.ScenarioId,
        MAX(cr.CalculationRunId) AS CalculationRunId
    FROM planning.CalculationRuns AS cr
    WHERE cr.RunStatus = N'Completed'
    GROUP BY cr.ScenarioId
)
SELECT
    s.ScenarioName,
    pw.WeekName,
    pw.WeekStartDate,
    m.MachineCode,
    m.MachineName,
    pn.PartNumber,
    pn.Description AS PartDescription,
    r.DemandUnits,
    r.AvailableHours,
    r.EffectiveHours,
    r.AppliedYield,
    r.AppliedUtilizationFactor,
    r.StandardTimeSeconds,
    r.TimeCapacityUnits,
    rt.RackCode,
    rt.RackName,
    r.UnitsPerRack,
    r.RequiredRackCount,
    r.AvailableRackCount,
    r.MissingRackCount,
    r.RackCapacityUnits,
    r.IsRackConstrained,
    r.CapacityUnits,
    r.RequiredHours,
    r.LoadPercent,
    r.RemainingUnits,
    r.CreatedAtUtc
FROM planning.CapacityResults AS r
INNER JOIN LatestRun AS lr
    ON lr.CalculationRunId = r.CalculationRunId
INNER JOIN planning.Scenarios AS s
    ON s.ScenarioId = r.ScenarioId
INNER JOIN planning.ProductionWeeks AS pw
    ON pw.ProductionWeekId = r.ProductionWeekId
INNER JOIN planning.Machines AS m
    ON m.MachineId = r.MachineId
INNER JOIN planning.PartNumbers AS pn
    ON pn.PartNumberId = r.PartNumberId
LEFT JOIN planning.RackTypes AS rt
    ON rt.RackTypeId = r.RackTypeId;
GO

INSERT INTO planning.RackTypes (RackCode, RackName, BaseUnitsPerRack)
SELECT N'CARRO-BASE', N'Carro base', 7
WHERE NOT EXISTS (SELECT 1 FROM planning.RackTypes WHERE RackCode = N'CARRO-BASE');
GO

INSERT INTO planning.RackAccessories (AccessoryCode, AccessoryName, AdditionalUnitsPerRack)
SELECT N'ACC-EXT-01', N'Accesorio extension', 3
WHERE NOT EXISTS (SELECT 1 FROM planning.RackAccessories WHERE AccessoryCode = N'ACC-EXT-01');
GO

DECLARE @RackTypeId int = (SELECT RackTypeId FROM planning.RackTypes WHERE RackCode = N'CARRO-BASE');
DECLARE @ScenarioId int = (SELECT ScenarioId FROM planning.Scenarios WHERE ScenarioName = N'Base semanal');
DECLARE @ProductionWeekId int = (
    SELECT TOP (1) ProductionWeekId
    FROM planning.ProductionWeeks
    WHERE ScenarioId = @ScenarioId
    ORDER BY ProductionWeekId
);
DECLARE @MachineId int = (SELECT MachineId FROM planning.Machines WHERE MachineCode = N'ASSY-1');
DECLARE @PartNumberId int = (SELECT PartNumberId FROM planning.PartNumbers WHERE PartNumber = N'68-6792');

IF @RackTypeId IS NOT NULL AND @MachineId IS NOT NULL AND @PartNumberId IS NOT NULL
BEGIN
    INSERT INTO planning.RackConfigurations (RackTypeId, MachineId, PartNumberId, UnitsPerRack, Notes)
    SELECT @RackTypeId, @MachineId, @PartNumberId, 7, N'Configuracion base para validar restriccion por carros.'
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM planning.RackConfigurations
        WHERE RackTypeId = @RackTypeId
          AND MachineId = @MachineId
          AND PartNumberId = @PartNumberId
          AND RackAccessoryId IS NULL
    );
END;

IF @ScenarioId IS NOT NULL AND @ProductionWeekId IS NOT NULL AND @MachineId IS NOT NULL AND @RackTypeId IS NOT NULL
BEGIN
    INSERT INTO planning.ScenarioRackInventories (ScenarioId, ProductionWeekId, MachineId, RackTypeId, AvailableRackCount, Notes)
    SELECT @ScenarioId, @ProductionWeekId, @MachineId, @RackTypeId, 200, N'Inventario inicial de ejemplo; ajustar con datos reales del Excel.'
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM planning.ScenarioRackInventories
        WHERE ScenarioId = @ScenarioId
          AND ProductionWeekId = @ProductionWeekId
          AND MachineId = @MachineId
          AND RackTypeId = @RackTypeId
    );
END;
GO
