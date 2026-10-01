/*
    ProductionCapacity - Initial SQL Server schema

    Run this script inside the target database.
    Optional:
        CREATE DATABASE ProductionCapacity;
        GO
        USE ProductionCapacity;
        GO
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'planning')
BEGIN
    EXEC(N'CREATE SCHEMA planning');
END;
GO

CREATE TABLE planning.Scenarios
(
    ScenarioId          int IDENTITY(1,1) NOT NULL,
    ScenarioName        nvarchar(120) NOT NULL,
    Description         nvarchar(500) NULL,
    IsBaseline          bit NOT NULL CONSTRAINT DF_Scenarios_IsBaseline DEFAULT (0),
    CreatedAtUtc        datetime2(0) NOT NULL CONSTRAINT DF_Scenarios_CreatedAtUtc DEFAULT (sysutcdatetime()),
    UpdatedAtUtc        datetime2(0) NOT NULL CONSTRAINT DF_Scenarios_UpdatedAtUtc DEFAULT (sysutcdatetime()),
    CONSTRAINT PK_Scenarios PRIMARY KEY CLUSTERED (ScenarioId),
    CONSTRAINT UQ_Scenarios_ScenarioName UNIQUE (ScenarioName)
);
GO

CREATE TABLE planning.ProductionWeeks
(
    ProductionWeekId        int IDENTITY(1,1) NOT NULL,
    ScenarioId              int NOT NULL,
    WeekName                nvarchar(30) NOT NULL,
    WeekStartDate           date NULL,
    WorkDays                decimal(9,4) NOT NULL,
    HoursPerShift           decimal(9,4) NOT NULL,
    ShiftsPerDay            int NOT NULL,
    HolidayDays             decimal(9,4) NOT NULL CONSTRAINT DF_ProductionWeeks_HolidayDays DEFAULT (0),
    OvertimeHours           decimal(9,4) NOT NULL CONSTRAINT DF_ProductionWeeks_OvertimeHours DEFAULT (0),
    PlannedDowntimeHours    decimal(9,4) NOT NULL CONSTRAINT DF_ProductionWeeks_PlannedDowntimeHours DEFAULT (0),
    CreatedAtUtc            datetime2(0) NOT NULL CONSTRAINT DF_ProductionWeeks_CreatedAtUtc DEFAULT (sysutcdatetime()),
    UpdatedAtUtc            datetime2(0) NOT NULL CONSTRAINT DF_ProductionWeeks_UpdatedAtUtc DEFAULT (sysutcdatetime()),
    CONSTRAINT PK_ProductionWeeks PRIMARY KEY CLUSTERED (ProductionWeekId),
    CONSTRAINT FK_ProductionWeeks_Scenarios FOREIGN KEY (ScenarioId)
        REFERENCES planning.Scenarios (ScenarioId),
    CONSTRAINT UQ_ProductionWeeks_Scenario_WeekName UNIQUE (ScenarioId, WeekName),
    CONSTRAINT CK_ProductionWeeks_WorkDays CHECK (WorkDays >= 0 AND WorkDays <= 7),
    CONSTRAINT CK_ProductionWeeks_HoursPerShift CHECK (HoursPerShift > 0 AND HoursPerShift <= 24),
    CONSTRAINT CK_ProductionWeeks_ShiftsPerDay CHECK (ShiftsPerDay > 0 AND ShiftsPerDay <= 4),
    CONSTRAINT CK_ProductionWeeks_HolidayDays CHECK (HolidayDays >= 0 AND HolidayDays <= 7),
    CONSTRAINT CK_ProductionWeeks_OvertimeHours CHECK (OvertimeHours >= 0),
    CONSTRAINT CK_ProductionWeeks_PlannedDowntimeHours CHECK (PlannedDowntimeHours >= 0)
);
GO

CREATE TABLE planning.Machines
(
    MachineId                   int IDENTITY(1,1) NOT NULL,
    MachineCode                 nvarchar(60) NOT NULL,
    MachineName                 nvarchar(160) NOT NULL,
    MachineType                 nvarchar(80) NULL,
    DefaultUtilizationFactor    decimal(9,6) NOT NULL,
    IsActive                    bit NOT NULL CONSTRAINT DF_Machines_IsActive DEFAULT (1),
    CreatedAtUtc                datetime2(0) NOT NULL CONSTRAINT DF_Machines_CreatedAtUtc DEFAULT (sysutcdatetime()),
    UpdatedAtUtc                datetime2(0) NOT NULL CONSTRAINT DF_Machines_UpdatedAtUtc DEFAULT (sysutcdatetime()),
    CONSTRAINT PK_Machines PRIMARY KEY CLUSTERED (MachineId),
    CONSTRAINT UQ_Machines_MachineCode UNIQUE (MachineCode),
    CONSTRAINT CK_Machines_DefaultUtilizationFactor CHECK (DefaultUtilizationFactor >= 0 AND DefaultUtilizationFactor <= 2)
);
GO

CREATE TABLE planning.PartNumbers
(
    PartNumberId        int IDENTITY(1,1) NOT NULL,
    PartNumber          nvarchar(80) NOT NULL,
    Description         nvarchar(240) NOT NULL,
    BusinessUnit        nvarchar(80) NULL,
    ProductFamily       nvarchar(120) NULL,
    DefaultYield        decimal(9,6) NOT NULL CONSTRAINT DF_PartNumbers_DefaultYield DEFAULT (1),
    IsActive            bit NOT NULL CONSTRAINT DF_PartNumbers_IsActive DEFAULT (1),
    CreatedAtUtc        datetime2(0) NOT NULL CONSTRAINT DF_PartNumbers_CreatedAtUtc DEFAULT (sysutcdatetime()),
    UpdatedAtUtc        datetime2(0) NOT NULL CONSTRAINT DF_PartNumbers_UpdatedAtUtc DEFAULT (sysutcdatetime()),
    CONSTRAINT PK_PartNumbers PRIMARY KEY CLUSTERED (PartNumberId),
    CONSTRAINT UQ_PartNumbers_PartNumber UNIQUE (PartNumber),
    CONSTRAINT CK_PartNumbers_DefaultYield CHECK (DefaultYield >= 0 AND DefaultYield <= 1.5)
);
GO

CREATE TABLE planning.PartCrossReferences
(
    PartCrossReferenceId    int IDENTITY(1,1) NOT NULL,
    PartNumberId            int NOT NULL,
    ReferenceType           nvarchar(40) NOT NULL,
    ReferenceValue          nvarchar(120) NOT NULL,
    CreatedAtUtc            datetime2(0) NOT NULL CONSTRAINT DF_PartCrossReferences_CreatedAtUtc DEFAULT (sysutcdatetime()),
    CONSTRAINT PK_PartCrossReferences PRIMARY KEY CLUSTERED (PartCrossReferenceId),
    CONSTRAINT FK_PartCrossReferences_PartNumbers FOREIGN KEY (PartNumberId)
        REFERENCES planning.PartNumbers (PartNumberId),
    CONSTRAINT UQ_PartCrossReferences_Type_Value UNIQUE (ReferenceType, ReferenceValue)
);
GO

CREATE TABLE planning.ProcessRoutes
(
    ProcessRouteId              int IDENTITY(1,1) NOT NULL,
    PartNumberId                int NOT NULL,
    MachineId                   int NOT NULL,
    OperationName               nvarchar(120) NOT NULL,
    SequenceNumber              int NOT NULL,
    StandardTimeSeconds         decimal(18,6) NOT NULL,
    DefaultUtilizationFactor    decimal(9,6) NULL,
    DefaultYield                decimal(9,6) NULL,
    IsCapacityConstrained       bit NOT NULL CONSTRAINT DF_ProcessRoutes_IsCapacityConstrained DEFAULT (1),
    CreatedAtUtc                datetime2(0) NOT NULL CONSTRAINT DF_ProcessRoutes_CreatedAtUtc DEFAULT (sysutcdatetime()),
    UpdatedAtUtc                datetime2(0) NOT NULL CONSTRAINT DF_ProcessRoutes_UpdatedAtUtc DEFAULT (sysutcdatetime()),
    CONSTRAINT PK_ProcessRoutes PRIMARY KEY CLUSTERED (ProcessRouteId),
    CONSTRAINT FK_ProcessRoutes_PartNumbers FOREIGN KEY (PartNumberId)
        REFERENCES planning.PartNumbers (PartNumberId),
    CONSTRAINT FK_ProcessRoutes_Machines FOREIGN KEY (MachineId)
        REFERENCES planning.Machines (MachineId),
    CONSTRAINT UQ_ProcessRoutes_Part_Machine_Operation UNIQUE (PartNumberId, MachineId, OperationName),
    CONSTRAINT CK_ProcessRoutes_SequenceNumber CHECK (SequenceNumber > 0),
    CONSTRAINT CK_ProcessRoutes_StandardTimeSeconds CHECK (StandardTimeSeconds > 0),
    CONSTRAINT CK_ProcessRoutes_DefaultUtilizationFactor CHECK (DefaultUtilizationFactor IS NULL OR (DefaultUtilizationFactor >= 0 AND DefaultUtilizationFactor <= 2)),
    CONSTRAINT CK_ProcessRoutes_DefaultYield CHECK (DefaultYield IS NULL OR (DefaultYield >= 0 AND DefaultYield <= 1.5))
);
GO

CREATE TABLE planning.ScenarioPartOverrides
(
    ScenarioPartOverrideId  int IDENTITY(1,1) NOT NULL,
    ScenarioId              int NOT NULL,
    PartNumberId            int NOT NULL,
    YieldOverride           decimal(9,6) NULL,
    Notes                   nvarchar(400) NULL,
    CreatedAtUtc            datetime2(0) NOT NULL CONSTRAINT DF_ScenarioPartOverrides_CreatedAtUtc DEFAULT (sysutcdatetime()),
    UpdatedAtUtc            datetime2(0) NOT NULL CONSTRAINT DF_ScenarioPartOverrides_UpdatedAtUtc DEFAULT (sysutcdatetime()),
    CONSTRAINT PK_ScenarioPartOverrides PRIMARY KEY CLUSTERED (ScenarioPartOverrideId),
    CONSTRAINT FK_ScenarioPartOverrides_Scenarios FOREIGN KEY (ScenarioId)
        REFERENCES planning.Scenarios (ScenarioId),
    CONSTRAINT FK_ScenarioPartOverrides_PartNumbers FOREIGN KEY (PartNumberId)
        REFERENCES planning.PartNumbers (PartNumberId),
    CONSTRAINT UQ_ScenarioPartOverrides_Scenario_Part UNIQUE (ScenarioId, PartNumberId),
    CONSTRAINT CK_ScenarioPartOverrides_YieldOverride CHECK (YieldOverride IS NULL OR (YieldOverride >= 0 AND YieldOverride <= 1.5))
);
GO

CREATE TABLE planning.ScenarioMachineOverrides
(
    ScenarioMachineOverrideId       int IDENTITY(1,1) NOT NULL,
    ScenarioId                      int NOT NULL,
    MachineId                       int NOT NULL,
    UtilizationFactorOverride       decimal(9,6) NULL,
    Notes                           nvarchar(400) NULL,
    CreatedAtUtc                    datetime2(0) NOT NULL CONSTRAINT DF_ScenarioMachineOverrides_CreatedAtUtc DEFAULT (sysutcdatetime()),
    UpdatedAtUtc                    datetime2(0) NOT NULL CONSTRAINT DF_ScenarioMachineOverrides_UpdatedAtUtc DEFAULT (sysutcdatetime()),
    CONSTRAINT PK_ScenarioMachineOverrides PRIMARY KEY CLUSTERED (ScenarioMachineOverrideId),
    CONSTRAINT FK_ScenarioMachineOverrides_Scenarios FOREIGN KEY (ScenarioId)
        REFERENCES planning.Scenarios (ScenarioId),
    CONSTRAINT FK_ScenarioMachineOverrides_Machines FOREIGN KEY (MachineId)
        REFERENCES planning.Machines (MachineId),
    CONSTRAINT UQ_ScenarioMachineOverrides_Scenario_Machine UNIQUE (ScenarioId, MachineId),
    CONSTRAINT CK_ScenarioMachineOverrides_UtilizationFactorOverride CHECK (UtilizationFactorOverride IS NULL OR (UtilizationFactorOverride >= 0 AND UtilizationFactorOverride <= 2))
);
GO

CREATE TABLE planning.ScenarioMachinePartOverrides
(
    ScenarioMachinePartOverrideId   int IDENTITY(1,1) NOT NULL,
    ScenarioId                      int NOT NULL,
    MachineId                       int NOT NULL,
    PartNumberId                    int NOT NULL,
    UtilizationFactorOverride       decimal(9,6) NULL,
    YieldOverride                   decimal(9,6) NULL,
    StandardTimeSecondsOverride     decimal(18,6) NULL,
    Notes                           nvarchar(400) NULL,
    CreatedAtUtc                    datetime2(0) NOT NULL CONSTRAINT DF_ScenarioMachinePartOverrides_CreatedAtUtc DEFAULT (sysutcdatetime()),
    UpdatedAtUtc                    datetime2(0) NOT NULL CONSTRAINT DF_ScenarioMachinePartOverrides_UpdatedAtUtc DEFAULT (sysutcdatetime()),
    CONSTRAINT PK_ScenarioMachinePartOverrides PRIMARY KEY CLUSTERED (ScenarioMachinePartOverrideId),
    CONSTRAINT FK_ScenarioMachinePartOverrides_Scenarios FOREIGN KEY (ScenarioId)
        REFERENCES planning.Scenarios (ScenarioId),
    CONSTRAINT FK_ScenarioMachinePartOverrides_Machines FOREIGN KEY (MachineId)
        REFERENCES planning.Machines (MachineId),
    CONSTRAINT FK_ScenarioMachinePartOverrides_PartNumbers FOREIGN KEY (PartNumberId)
        REFERENCES planning.PartNumbers (PartNumberId),
    CONSTRAINT UQ_ScenarioMachinePartOverrides_Scenario_Machine_Part UNIQUE (ScenarioId, MachineId, PartNumberId),
    CONSTRAINT CK_ScenarioMachinePartOverrides_UtilizationFactorOverride CHECK (UtilizationFactorOverride IS NULL OR (UtilizationFactorOverride >= 0 AND UtilizationFactorOverride <= 2)),
    CONSTRAINT CK_ScenarioMachinePartOverrides_YieldOverride CHECK (YieldOverride IS NULL OR (YieldOverride >= 0 AND YieldOverride <= 1.5)),
    CONSTRAINT CK_ScenarioMachinePartOverrides_StandardTimeSecondsOverride CHECK (StandardTimeSecondsOverride IS NULL OR StandardTimeSecondsOverride > 0)
);
GO

CREATE TABLE planning.WeeklyDemand
(
    WeeklyDemandId      int IDENTITY(1,1) NOT NULL,
    ScenarioId          int NOT NULL,
    ProductionWeekId    int NOT NULL,
    PartNumberId        int NOT NULL,
    DemandUnits         decimal(18,4) NOT NULL,
    CreatedAtUtc        datetime2(0) NOT NULL CONSTRAINT DF_WeeklyDemand_CreatedAtUtc DEFAULT (sysutcdatetime()),
    UpdatedAtUtc        datetime2(0) NOT NULL CONSTRAINT DF_WeeklyDemand_UpdatedAtUtc DEFAULT (sysutcdatetime()),
    CONSTRAINT PK_WeeklyDemand PRIMARY KEY CLUSTERED (WeeklyDemandId),
    CONSTRAINT FK_WeeklyDemand_Scenarios FOREIGN KEY (ScenarioId)
        REFERENCES planning.Scenarios (ScenarioId),
    CONSTRAINT FK_WeeklyDemand_ProductionWeeks FOREIGN KEY (ProductionWeekId)
        REFERENCES planning.ProductionWeeks (ProductionWeekId),
    CONSTRAINT FK_WeeklyDemand_PartNumbers FOREIGN KEY (PartNumberId)
        REFERENCES planning.PartNumbers (PartNumberId),
    CONSTRAINT UQ_WeeklyDemand_Scenario_Week_Part UNIQUE (ScenarioId, ProductionWeekId, PartNumberId),
    CONSTRAINT CK_WeeklyDemand_DemandUnits CHECK (DemandUnits >= 0)
);
GO

CREATE TABLE planning.CalculationRuns
(
    CalculationRunId    bigint IDENTITY(1,1) NOT NULL,
    ScenarioId          int NOT NULL,
    RunStatus           nvarchar(30) NOT NULL CONSTRAINT DF_CalculationRuns_RunStatus DEFAULT (N'Completed'),
    StartedAtUtc        datetime2(0) NOT NULL CONSTRAINT DF_CalculationRuns_StartedAtUtc DEFAULT (sysutcdatetime()),
    CompletedAtUtc      datetime2(0) NULL,
    Notes               nvarchar(500) NULL,
    CONSTRAINT PK_CalculationRuns PRIMARY KEY CLUSTERED (CalculationRunId),
    CONSTRAINT FK_CalculationRuns_Scenarios FOREIGN KEY (ScenarioId)
        REFERENCES planning.Scenarios (ScenarioId),
    CONSTRAINT CK_CalculationRuns_RunStatus CHECK (RunStatus IN (N'Running', N'Completed', N'Failed'))
);
GO

CREATE TABLE planning.CapacityResults
(
    CapacityResultId            bigint IDENTITY(1,1) NOT NULL,
    CalculationRunId            bigint NOT NULL,
    ScenarioId                  int NOT NULL,
    ProductionWeekId            int NOT NULL,
    PartNumberId                int NOT NULL,
    MachineId                   int NOT NULL,
    ProcessRouteId              int NULL,
    DemandUnits                 decimal(18,4) NOT NULL,
    AvailableHours              decimal(18,6) NOT NULL,
    EffectiveHours              decimal(18,6) NOT NULL,
    AppliedYield                decimal(9,6) NOT NULL,
    AppliedUtilizationFactor    decimal(9,6) NOT NULL,
    StandardTimeSeconds         decimal(18,6) NOT NULL,
    CapacityUnits               decimal(18,4) NOT NULL,
    RequiredHours               decimal(18,6) NOT NULL,
    LoadPercent                 decimal(18,6) NOT NULL,
    RemainingUnits              decimal(18,4) NOT NULL,
    CreatedAtUtc                datetime2(0) NOT NULL CONSTRAINT DF_CapacityResults_CreatedAtUtc DEFAULT (sysutcdatetime()),
    CONSTRAINT PK_CapacityResults PRIMARY KEY CLUSTERED (CapacityResultId),
    CONSTRAINT FK_CapacityResults_CalculationRuns FOREIGN KEY (CalculationRunId)
        REFERENCES planning.CalculationRuns (CalculationRunId),
    CONSTRAINT FK_CapacityResults_Scenarios FOREIGN KEY (ScenarioId)
        REFERENCES planning.Scenarios (ScenarioId),
    CONSTRAINT FK_CapacityResults_ProductionWeeks FOREIGN KEY (ProductionWeekId)
        REFERENCES planning.ProductionWeeks (ProductionWeekId),
    CONSTRAINT FK_CapacityResults_PartNumbers FOREIGN KEY (PartNumberId)
        REFERENCES planning.PartNumbers (PartNumberId),
    CONSTRAINT FK_CapacityResults_Machines FOREIGN KEY (MachineId)
        REFERENCES planning.Machines (MachineId),
    CONSTRAINT FK_CapacityResults_ProcessRoutes FOREIGN KEY (ProcessRouteId)
        REFERENCES planning.ProcessRoutes (ProcessRouteId),
    CONSTRAINT CK_CapacityResults_DemandUnits CHECK (DemandUnits >= 0),
    CONSTRAINT CK_CapacityResults_AvailableHours CHECK (AvailableHours >= 0),
    CONSTRAINT CK_CapacityResults_EffectiveHours CHECK (EffectiveHours >= 0),
    CONSTRAINT CK_CapacityResults_AppliedYield CHECK (AppliedYield >= 0 AND AppliedYield <= 1.5),
    CONSTRAINT CK_CapacityResults_AppliedUtilizationFactor CHECK (AppliedUtilizationFactor >= 0 AND AppliedUtilizationFactor <= 2),
    CONSTRAINT CK_CapacityResults_StandardTimeSeconds CHECK (StandardTimeSeconds > 0)
);
GO

CREATE INDEX IX_ProductionWeeks_ScenarioId
    ON planning.ProductionWeeks (ScenarioId);
GO

CREATE INDEX IX_ProcessRoutes_PartNumberId
    ON planning.ProcessRoutes (PartNumberId);
GO

CREATE INDEX IX_ProcessRoutes_MachineId
    ON planning.ProcessRoutes (MachineId);
GO

CREATE INDEX IX_WeeklyDemand_Scenario_Week
    ON planning.WeeklyDemand (ScenarioId, ProductionWeekId);
GO

CREATE INDEX IX_CalculationRuns_ScenarioId_StartedAtUtc
    ON planning.CalculationRuns (ScenarioId, StartedAtUtc DESC);
GO

CREATE INDEX IX_CapacityResults_Run
    ON planning.CapacityResults (CalculationRunId);
GO

CREATE INDEX IX_CapacityResults_Scenario_Week_Machine
    ON planning.CapacityResults (ScenarioId, ProductionWeekId, MachineId)
    INCLUDE (LoadPercent, RemainingUnits, CapacityUnits, DemandUnits);
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
    ON pn.PartNumberId = r.PartNumberId;
GO

INSERT INTO planning.Scenarios (ScenarioName, Description, IsBaseline)
SELECT N'Base semanal', N'Escenario inicial para validar el MVP contra el motor de calculo.', 1
WHERE NOT EXISTS (SELECT 1 FROM planning.Scenarios WHERE ScenarioName = N'Base semanal');
GO

DECLARE @ScenarioId int =
(
    SELECT ScenarioId
    FROM planning.Scenarios
    WHERE ScenarioName = N'Base semanal'
);

INSERT INTO planning.ProductionWeeks
(
    ScenarioId,
    WeekName,
    WeekStartDate,
    WorkDays,
    HoursPerShift,
    ShiftsPerDay,
    HolidayDays,
    OvertimeHours,
    PlannedDowntimeHours
)
SELECT @ScenarioId, N'WK13', NULL, 5.66, 7.5, 3, 0, 0, 0
WHERE NOT EXISTS
(
    SELECT 1
    FROM planning.ProductionWeeks
    WHERE ScenarioId = @ScenarioId
      AND WeekName = N'WK13'
);

INSERT INTO planning.Machines (MachineCode, MachineName, MachineType, DefaultUtilizationFactor)
SELECT v.MachineCode, v.MachineName, v.MachineType, v.DefaultUtilizationFactor
FROM
(
    VALUES
        (N'ASSY-1', N'ASSY 1', N'Assembly', 0.800000),
        (N'ASSY-2', N'ASSY 2', N'Assembly', 0.800000),
        (N'PACK-1', N'PACK 1', N'Pack', 0.750000),
        (N'RFID-LONGBALL', N'RFID LONGBALL', N'Test', 0.900000),
        (N'PCBST-LONGBALL', N'PCBST LONGBALL', N'Test', 0.900000)
) AS v(MachineCode, MachineName, MachineType, DefaultUtilizationFactor)
WHERE NOT EXISTS
(
    SELECT 1
    FROM planning.Machines AS m
    WHERE m.MachineCode = v.MachineCode
);

INSERT INTO planning.PartNumbers (PartNumber, Description, BusinessUnit, ProductFamily, DefaultYield)
SELECT v.PartNumber, v.Description, v.BusinessUnit, v.ProductFamily, v.DefaultYield
FROM
(
    VALUES
        (N'68-6792', N'FC3', N'ERBU', N'FC3', 1.000000),
        (N'68-6153', N'FC3-S', N'ERBU', N'FC3', 0.852000),
        (N'68-103450', N'Screwball', N'LONGBALL', N'Longball', 0.950000),
        (N'68-103948', N'Change up 12 CR', N'LONGBALL', N'Longball', 0.950000)
) AS v(PartNumber, Description, BusinessUnit, ProductFamily, DefaultYield)
WHERE NOT EXISTS
(
    SELECT 1
    FROM planning.PartNumbers AS pn
    WHERE pn.PartNumber = v.PartNumber
);

INSERT INTO planning.ProcessRoutes (PartNumberId, MachineId, OperationName, SequenceNumber, StandardTimeSeconds)
SELECT pn.PartNumberId, m.MachineId, v.OperationName, v.SequenceNumber, v.StandardTimeSeconds
FROM
(
    VALUES
        (N'68-6792', N'ASSY-1', N'ASSY', 1, 300.000000),
        (N'68-6153', N'ASSY-2', N'ASSY', 1, 161.497000),
        (N'68-6792', N'PACK-1', N'PACK', 2, 91.111000),
        (N'68-103450', N'RFID-LONGBALL', N'RFID', 1, 60.000000),
        (N'68-103948', N'PCBST-LONGBALL', N'PCBST', 2, 51.964000)
) AS v(PartNumber, MachineCode, OperationName, SequenceNumber, StandardTimeSeconds)
INNER JOIN planning.PartNumbers AS pn
    ON pn.PartNumber = v.PartNumber
INNER JOIN planning.Machines AS m
    ON m.MachineCode = v.MachineCode
WHERE NOT EXISTS
(
    SELECT 1
    FROM planning.ProcessRoutes AS pr
    WHERE pr.PartNumberId = pn.PartNumberId
      AND pr.MachineId = m.MachineId
      AND pr.OperationName = v.OperationName
);

DECLARE @ProductionWeekId int =
(
    SELECT ProductionWeekId
    FROM planning.ProductionWeeks
    WHERE ScenarioId = @ScenarioId
      AND WeekName = N'WK13'
);

INSERT INTO planning.WeeklyDemand (ScenarioId, ProductionWeekId, PartNumberId, DemandUnits)
SELECT @ScenarioId, @ProductionWeekId, pn.PartNumberId, v.DemandUnits
FROM
(
    VALUES
        (N'68-6792', 1000.0000),
        (N'68-6153', 1250.0000),
        (N'68-103450', 1000.0000),
        (N'68-103948', 650.0000)
) AS v(PartNumber, DemandUnits)
INNER JOIN planning.PartNumbers AS pn
    ON pn.PartNumber = v.PartNumber
WHERE NOT EXISTS
(
    SELECT 1
    FROM planning.WeeklyDemand AS wd
    WHERE wd.ScenarioId = @ScenarioId
      AND wd.ProductionWeekId = @ProductionWeekId
      AND wd.PartNumberId = pn.PartNumberId
);
GO
