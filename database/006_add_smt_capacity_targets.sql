IF SCHEMA_ID(N'planning') IS NULL
BEGIN
    EXEC(N'CREATE SCHEMA planning');
END;
GO

IF OBJECT_ID(N'planning.SmtCapacityTargets', N'U') IS NULL
BEGIN
    CREATE TABLE planning.SmtCapacityTargets
    (
        SmtCapacityTargetId          int IDENTITY(1,1) NOT NULL,
        ScenarioId                   int NOT NULL,
        ProductionWeekId             int NOT NULL,
        OperationId                  int NOT NULL,
        ProductFamily                nvarchar(120) NOT NULL,
        LineCode                     nvarchar(60) NOT NULL,
        ExpectedAssignedDemandUnits  decimal(18,6) NOT NULL,
        ExpectedMaxCapacityUnits     decimal(18,6) NOT NULL,
        ExpectedUtilizationPercent   decimal(18,6) NOT NULL,
        ExpectedDeltaCapacityUnits   decimal(18,6) NOT NULL,
        SourceReference              nvarchar(160) NULL,
        Notes                        nvarchar(400) NULL,
        CONSTRAINT PK_SmtCapacityTargets PRIMARY KEY CLUSTERED (SmtCapacityTargetId),
        CONSTRAINT FK_SmtCapacityTargets_Scenarios FOREIGN KEY (ScenarioId)
            REFERENCES planning.Scenarios (ScenarioId),
        CONSTRAINT FK_SmtCapacityTargets_ProductionWeeks FOREIGN KEY (ProductionWeekId)
            REFERENCES planning.ProductionWeeks (ProductionWeekId),
        CONSTRAINT FK_SmtCapacityTargets_Operations FOREIGN KEY (OperationId)
            REFERENCES planning.Operations (OperationId),
        CONSTRAINT UQ_SmtCapacityTargets_Scenario_Week_Operation_Family_Line UNIQUE
            (ScenarioId, ProductionWeekId, OperationId, ProductFamily, LineCode)
    );
END;
GO

/*
    Uso:
    1. Ejecuta este script despues de 005_add_smt_capacity.sql.
    2. Por default solo crea la tabla. El importador FMD SMT carga los targets reales desde la hoja Capacity SMT.
    3. Cambia @SeedTargetsFromSystem a 1 solo si necesitas crear una linea base temporal desde el calculo actual del sistema.
*/
DECLARE @ScenarioName nvarchar(120) = N'Base FMD SMT';
DECLARE @SeedTargetsFromSystem bit = 0;
DECLARE @ChangeoverHoursPerModel decimal(18,6) = 4.000000;

IF @SeedTargetsFromSystem = 1
BEGIN
    ;WITH Calculated AS
    (
        SELECT
            assignment.ScenarioId,
            assignment.ProductionWeekId,
            assignment.OperationId,
            assignment.ProductFamily,
            assignment.LineCode,
            CAST(CEILING((COALESCE(volume.WeeklyMps, 0) * assignment.LineDistribution) * 1000.0) / 1000.0 AS decimal(18,6))
                AS ExpectedAssignedDemandUnits,
            CAST(CEILING((rate.UnitsPerHour * available.AvailableHours) * 1000.0) / 1000.0 AS decimal(18,6))
                AS ExpectedMaxCapacityUnits,
            CAST(
                CASE
                    WHEN (rate.UnitsPerHour * available.AvailableHours) = 0 THEN 0
                    ELSE CEILING(((COALESCE(volume.WeeklyMps, 0) * assignment.LineDistribution) / (rate.UnitsPerHour * available.AvailableHours) * 100.0) * 1000.0) / 1000.0
                END
                AS decimal(18,6)) AS ExpectedUtilizationPercent,
            CAST(CEILING(((rate.UnitsPerHour * available.AvailableHours) - (COALESCE(volume.WeeklyMps, 0) * assignment.LineDistribution)) * 1000.0) / 1000.0 AS decimal(18,6))
                AS ExpectedDeltaCapacityUnits,
            CONCAT(N'Baseline sistema ', week.WeekName, N' ', assignment.LineCode, N' ', assignment.ProductFamily) AS SourceReference
        FROM planning.SmtLineAssignments assignment
        INNER JOIN planning.Scenarios scenario
            ON scenario.ScenarioId = assignment.ScenarioId
        INNER JOIN planning.ProductionWeeks week
            ON week.ProductionWeekId = assignment.ProductionWeekId
        INNER JOIN planning.OperationWeeklyCapacities capacity
            ON capacity.ScenarioId = assignment.ScenarioId
            AND capacity.ProductionWeekId = assignment.ProductionWeekId
            AND capacity.OperationId = assignment.OperationId
        INNER JOIN planning.SmtFamilyRates rate
            ON rate.OperationId = assignment.OperationId
            AND rate.ProductFamily = assignment.ProductFamily
            AND rate.IsActive = 1
        LEFT JOIN planning.SmtFamilyVolumes volume
            ON volume.ScenarioId = assignment.ScenarioId
            AND volume.ProductionWeekId = assignment.ProductionWeekId
            AND volume.ProductFamily = assignment.ProductFamily
        CROSS APPLY
        (
            SELECT
                CASE
                    WHEN (((capacity.WorkDays - capacity.HolidayDays) * capacity.HoursPerShift * capacity.ShiftsPerDay * capacity.EquipmentQuantity)
                        + capacity.OvertimeHours
                        - capacity.PlannedDowntimeHours
                        - (rate.ChangeoverCount * @ChangeoverHoursPerModel)) < 0
                    THEN CAST(0 AS decimal(18,6))
                    ELSE CAST(CEILING(((((capacity.WorkDays - capacity.HolidayDays) * capacity.HoursPerShift * capacity.ShiftsPerDay * capacity.EquipmentQuantity)
                        + capacity.OvertimeHours
                        - capacity.PlannedDowntimeHours
                        - (rate.ChangeoverCount * @ChangeoverHoursPerModel)) * 1000.0)) / 1000.0 AS decimal(18,6))
                END AS AvailableHours
        ) available
        WHERE scenario.ScenarioName = @ScenarioName
            AND assignment.IsActive = 1
    )
    MERGE planning.SmtCapacityTargets AS target
    USING Calculated AS source
        ON target.ScenarioId = source.ScenarioId
        AND target.ProductionWeekId = source.ProductionWeekId
        AND target.OperationId = source.OperationId
        AND target.ProductFamily = source.ProductFamily
        AND target.LineCode = source.LineCode
    WHEN MATCHED THEN
        UPDATE SET
            ExpectedAssignedDemandUnits = source.ExpectedAssignedDemandUnits,
            ExpectedMaxCapacityUnits = source.ExpectedMaxCapacityUnits,
            ExpectedUtilizationPercent = source.ExpectedUtilizationPercent,
            ExpectedDeltaCapacityUnits = source.ExpectedDeltaCapacityUnits,
            SourceReference = source.SourceReference,
            Notes = N'Target temporal generado desde el calculo actual del sistema.'
    WHEN NOT MATCHED THEN
        INSERT
        (
            ScenarioId,
            ProductionWeekId,
            OperationId,
            ProductFamily,
            LineCode,
            ExpectedAssignedDemandUnits,
            ExpectedMaxCapacityUnits,
            ExpectedUtilizationPercent,
            ExpectedDeltaCapacityUnits,
            SourceReference,
            Notes
        )
        VALUES
        (
            source.ScenarioId,
            source.ProductionWeekId,
            source.OperationId,
            source.ProductFamily,
            source.LineCode,
            source.ExpectedAssignedDemandUnits,
            source.ExpectedMaxCapacityUnits,
            source.ExpectedUtilizationPercent,
            source.ExpectedDeltaCapacityUnits,
            source.SourceReference,
            N'Target temporal generado desde el calculo actual del sistema.'
        );
END;

SELECT
    scenario.ScenarioName,
    COUNT(*) AS TargetRows
FROM planning.SmtCapacityTargets target
INNER JOIN planning.Scenarios scenario
    ON scenario.ScenarioId = target.ScenarioId
WHERE scenario.ScenarioName = @ScenarioName
GROUP BY scenario.ScenarioName;

SELECT TOP (100)
    week.WeekName,
    operation.OperationCode,
    target.LineCode,
    target.ProductFamily,
    target.ExpectedAssignedDemandUnits,
    target.ExpectedMaxCapacityUnits,
    target.ExpectedUtilizationPercent,
    target.ExpectedDeltaCapacityUnits,
    target.SourceReference,
    target.Notes
FROM planning.SmtCapacityTargets target
INNER JOIN planning.ProductionWeeks week
    ON week.ProductionWeekId = target.ProductionWeekId
INNER JOIN planning.Operations operation
    ON operation.OperationId = target.OperationId
INNER JOIN planning.Scenarios scenario
    ON scenario.ScenarioId = target.ScenarioId
WHERE scenario.ScenarioName = @ScenarioName
ORDER BY week.ProductionWeekId, target.LineCode, target.ProductFamily;
GO
