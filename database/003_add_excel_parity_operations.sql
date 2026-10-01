/*
    ProductionCapacity - Excel parity operation model

    Run this script inside the existing ProductionCapacity database after:
        001_create_planning_schema.sql
        002_add_rack_constraints.sql
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'planning.Operations', N'U') IS NULL
BEGIN
    CREATE TABLE planning.Operations
    (
        OperationId         int IDENTITY(1,1) NOT NULL,
        OperationCode       nvarchar(60) NOT NULL,
        OperationName       nvarchar(160) NOT NULL,
        OperationFamily     nvarchar(120) NULL,
        SequenceNumber      int NOT NULL,
        IsActive            bit NOT NULL CONSTRAINT DF_Operations_IsActive DEFAULT (1),
        CreatedAtUtc        datetime2(0) NOT NULL CONSTRAINT DF_Operations_CreatedAtUtc DEFAULT (sysutcdatetime()),
        UpdatedAtUtc        datetime2(0) NOT NULL CONSTRAINT DF_Operations_UpdatedAtUtc DEFAULT (sysutcdatetime()),
        CONSTRAINT PK_Operations PRIMARY KEY CLUSTERED (OperationId),
        CONSTRAINT UQ_Operations_OperationCode UNIQUE (OperationCode),
        CONSTRAINT CK_Operations_SequenceNumber CHECK (SequenceNumber > 0)
    );
END;
GO

IF OBJECT_ID(N'planning.OperationWeeklyCapacities', N'U') IS NULL
BEGIN
    CREATE TABLE planning.OperationWeeklyCapacities
    (
        OperationWeeklyCapacityId int IDENTITY(1,1) NOT NULL,
        ScenarioId                int NOT NULL,
        ProductionWeekId          int NOT NULL,
        OperationId               int NOT NULL,
        WorkDays                  decimal(9,4) NOT NULL,
        HoursPerShift             decimal(9,4) NOT NULL,
        ShiftsPerDay              int NOT NULL,
        EquipmentQuantity         decimal(18,4) NOT NULL,
        HolidayDays               decimal(9,4) NOT NULL CONSTRAINT DF_OperationWeeklyCapacities_HolidayDays DEFAULT (0),
        OvertimeHours             decimal(18,4) NOT NULL CONSTRAINT DF_OperationWeeklyCapacities_OvertimeHours DEFAULT (0),
        PlannedDowntimeHours      decimal(18,4) NOT NULL CONSTRAINT DF_OperationWeeklyCapacities_PlannedDowntimeHours DEFAULT (0),
        CreatedAtUtc              datetime2(0) NOT NULL CONSTRAINT DF_OperationWeeklyCapacities_CreatedAtUtc DEFAULT (sysutcdatetime()),
        UpdatedAtUtc              datetime2(0) NOT NULL CONSTRAINT DF_OperationWeeklyCapacities_UpdatedAtUtc DEFAULT (sysutcdatetime()),
        CONSTRAINT PK_OperationWeeklyCapacities PRIMARY KEY CLUSTERED (OperationWeeklyCapacityId),
        CONSTRAINT FK_OperationWeeklyCapacities_Scenarios FOREIGN KEY (ScenarioId)
            REFERENCES planning.Scenarios (ScenarioId),
        CONSTRAINT FK_OperationWeeklyCapacities_ProductionWeeks FOREIGN KEY (ProductionWeekId)
            REFERENCES planning.ProductionWeeks (ProductionWeekId),
        CONSTRAINT FK_OperationWeeklyCapacities_Operations FOREIGN KEY (OperationId)
            REFERENCES planning.Operations (OperationId),
        CONSTRAINT UQ_OperationWeeklyCapacities_Scenario_Week_Operation UNIQUE (ScenarioId, ProductionWeekId, OperationId),
        CONSTRAINT CK_OperationWeeklyCapacities_WorkDays CHECK (WorkDays >= 0 AND WorkDays <= 7),
        CONSTRAINT CK_OperationWeeklyCapacities_HoursPerShift CHECK (HoursPerShift > 0 AND HoursPerShift <= 24),
        CONSTRAINT CK_OperationWeeklyCapacities_ShiftsPerDay CHECK (ShiftsPerDay > 0 AND ShiftsPerDay <= 4),
        CONSTRAINT CK_OperationWeeklyCapacities_EquipmentQuantity CHECK (EquipmentQuantity > 0),
        CONSTRAINT CK_OperationWeeklyCapacities_HolidayDays CHECK (HolidayDays >= 0 AND HolidayDays <= 7),
        CONSTRAINT CK_OperationWeeklyCapacities_OvertimeHours CHECK (OvertimeHours >= 0),
        CONSTRAINT CK_OperationWeeklyCapacities_PlannedDowntimeHours CHECK (PlannedDowntimeHours >= 0)
    );
END;
GO

IF OBJECT_ID(N'planning.OperationRates', N'U') IS NULL
BEGIN
    CREATE TABLE planning.OperationRates
    (
        OperationRateId       int IDENTITY(1,1) NOT NULL,
        PartNumberId          int NOT NULL,
        OperationId           int NOT NULL,
        StandardTimeSeconds   decimal(18,6) NOT NULL,
        YieldFactor           decimal(9,6) NOT NULL CONSTRAINT DF_OperationRates_YieldFactor DEFAULT (1),
        UtilizationFactor     decimal(9,6) NOT NULL CONSTRAINT DF_OperationRates_UtilizationFactor DEFAULT (1),
        EfficiencyFactor      decimal(9,6) NOT NULL CONSTRAINT DF_OperationRates_EfficiencyFactor DEFAULT (1),
        MaxCapacityUnits      decimal(18,4) NULL,
        FixedHeadcount        decimal(18,4) NULL,
        IsActive              bit NOT NULL CONSTRAINT DF_OperationRates_IsActive DEFAULT (1),
        CreatedAtUtc          datetime2(0) NOT NULL CONSTRAINT DF_OperationRates_CreatedAtUtc DEFAULT (sysutcdatetime()),
        UpdatedAtUtc          datetime2(0) NOT NULL CONSTRAINT DF_OperationRates_UpdatedAtUtc DEFAULT (sysutcdatetime()),
        CONSTRAINT PK_OperationRates PRIMARY KEY CLUSTERED (OperationRateId),
        CONSTRAINT FK_OperationRates_PartNumbers FOREIGN KEY (PartNumberId)
            REFERENCES planning.PartNumbers (PartNumberId),
        CONSTRAINT FK_OperationRates_Operations FOREIGN KEY (OperationId)
            REFERENCES planning.Operations (OperationId),
        CONSTRAINT UQ_OperationRates_Part_Operation UNIQUE (PartNumberId, OperationId),
        CONSTRAINT CK_OperationRates_StandardTimeSeconds CHECK (StandardTimeSeconds > 0),
        CONSTRAINT CK_OperationRates_YieldFactor CHECK (YieldFactor >= 0 AND YieldFactor <= 1.5),
        CONSTRAINT CK_OperationRates_UtilizationFactor CHECK (UtilizationFactor >= 0 AND UtilizationFactor <= 2),
        CONSTRAINT CK_OperationRates_EfficiencyFactor CHECK (EfficiencyFactor >= 0 AND EfficiencyFactor <= 2),
        CONSTRAINT CK_OperationRates_MaxCapacityUnits CHECK (MaxCapacityUnits IS NULL OR MaxCapacityUnits >= 0),
        CONSTRAINT CK_OperationRates_FixedHeadcount CHECK (FixedHeadcount IS NULL OR FixedHeadcount >= 0)
    );
END;
GO

IF OBJECT_ID(N'planning.OperationCapacityResults', N'U') IS NULL
BEGIN
    CREATE TABLE planning.OperationCapacityResults
    (
        OperationCapacityResultId bigint IDENTITY(1,1) NOT NULL,
        CalculationRunId          bigint NULL,
        ScenarioId                int NOT NULL,
        ProductionWeekId          int NOT NULL,
        PartNumberId              int NOT NULL,
        OperationId               int NOT NULL,
        DemandUnits               decimal(18,4) NOT NULL,
        AvailableHours            decimal(18,6) NOT NULL,
        EffectiveUnitsPerHour     decimal(18,6) NOT NULL,
        LoadHours                 decimal(18,6) NOT NULL,
        TimeCapacityUnits         decimal(18,4) NOT NULL,
        RemainingHours            decimal(18,6) NOT NULL,
        LoadPercent               decimal(18,6) NOT NULL,
        CreatedAtUtc              datetime2(0) NOT NULL CONSTRAINT DF_OperationCapacityResults_CreatedAtUtc DEFAULT (sysutcdatetime()),
        CONSTRAINT PK_OperationCapacityResults PRIMARY KEY CLUSTERED (OperationCapacityResultId),
        CONSTRAINT FK_OperationCapacityResults_CalculationRuns FOREIGN KEY (CalculationRunId)
            REFERENCES planning.CalculationRuns (CalculationRunId),
        CONSTRAINT FK_OperationCapacityResults_Scenarios FOREIGN KEY (ScenarioId)
            REFERENCES planning.Scenarios (ScenarioId),
        CONSTRAINT FK_OperationCapacityResults_ProductionWeeks FOREIGN KEY (ProductionWeekId)
            REFERENCES planning.ProductionWeeks (ProductionWeekId),
        CONSTRAINT FK_OperationCapacityResults_PartNumbers FOREIGN KEY (PartNumberId)
            REFERENCES planning.PartNumbers (PartNumberId),
        CONSTRAINT FK_OperationCapacityResults_Operations FOREIGN KEY (OperationId)
            REFERENCES planning.Operations (OperationId),
        CONSTRAINT CK_OperationCapacityResults_DemandUnits CHECK (DemandUnits >= 0),
        CONSTRAINT CK_OperationCapacityResults_AvailableHours CHECK (AvailableHours >= 0),
        CONSTRAINT CK_OperationCapacityResults_EffectiveUnitsPerHour CHECK (EffectiveUnitsPerHour >= 0),
        CONSTRAINT CK_OperationCapacityResults_LoadHours CHECK (LoadHours >= 0),
        CONSTRAINT CK_OperationCapacityResults_TimeCapacityUnits CHECK (TimeCapacityUnits >= 0)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OperationRates_Operation' AND object_id = OBJECT_ID(N'planning.OperationRates'))
BEGIN
    CREATE INDEX IX_OperationRates_Operation
        ON planning.OperationRates (OperationId)
        INCLUDE (PartNumberId, StandardTimeSeconds, YieldFactor, UtilizationFactor, EfficiencyFactor, IsActive);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OperationWeeklyCapacities_Scenario_Week' AND object_id = OBJECT_ID(N'planning.OperationWeeklyCapacities'))
BEGIN
    CREATE INDEX IX_OperationWeeklyCapacities_Scenario_Week
        ON planning.OperationWeeklyCapacities (ScenarioId, ProductionWeekId)
        INCLUDE (OperationId, EquipmentQuantity, WorkDays, HoursPerShift, ShiftsPerDay);
END;
GO

CREATE OR ALTER VIEW planning.vwOperationPlanningMatrix
AS
SELECT
    s.ScenarioName,
    pw.WeekName,
    pw.WeekStartDate,
    pn.PartNumber,
    pn.Description AS PartDescription,
    wd.DemandUnits,
    op.OperationCode,
    op.OperationName,
    owc.WorkDays,
    owc.HoursPerShift,
    owc.ShiftsPerDay,
    owc.EquipmentQuantity,
    CAST(((owc.WorkDays - owc.HolidayDays) * owc.HoursPerShift * owc.ShiftsPerDay * owc.EquipmentQuantity) + owc.OvertimeHours - owc.PlannedDowntimeHours AS decimal(18,6)) AS AvailableHours,
    rate.StandardTimeSeconds,
    rate.YieldFactor,
    rate.UtilizationFactor,
    rate.EfficiencyFactor,
    CAST((3600.0 / rate.StandardTimeSeconds) * rate.YieldFactor * rate.UtilizationFactor * rate.EfficiencyFactor AS decimal(18,6)) AS EffectiveUnitsPerHour,
    CAST(wd.DemandUnits / NULLIF((3600.0 / rate.StandardTimeSeconds) * rate.YieldFactor * rate.UtilizationFactor * rate.EfficiencyFactor, 0) AS decimal(18,6)) AS LoadHours
FROM planning.WeeklyDemand AS wd
INNER JOIN planning.Scenarios AS s
    ON s.ScenarioId = wd.ScenarioId
INNER JOIN planning.ProductionWeeks AS pw
    ON pw.ProductionWeekId = wd.ProductionWeekId
INNER JOIN planning.PartNumbers AS pn
    ON pn.PartNumberId = wd.PartNumberId
INNER JOIN planning.OperationRates AS rate
    ON rate.PartNumberId = wd.PartNumberId
   AND rate.IsActive = 1
INNER JOIN planning.Operations AS op
    ON op.OperationId = rate.OperationId
   AND op.IsActive = 1
INNER JOIN planning.OperationWeeklyCapacities AS owc
    ON owc.ScenarioId = wd.ScenarioId
   AND owc.ProductionWeekId = wd.ProductionWeekId
   AND owc.OperationId = rate.OperationId;
GO
