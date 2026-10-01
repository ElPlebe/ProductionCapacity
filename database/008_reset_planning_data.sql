/*
    ProductionCapacity - Reset de datos de planeacion

    Uso:
    1. Revisa el alcance antes de ejecutar.
    2. Cambia @ConfirmReset a 1.
    3. Ejecuta dentro de la base ProductionCapacity.

    @ResetScope:
    - 'SMT_ONLY': borra datos del escenario Base FMD SMT y tablas SMT; conserva otros catalogos.
    - 'ALL': borra toda la informacion de usuario del esquema planning; conserva las tablas.
*/

SET XACT_ABORT ON;
GO

DECLARE @ConfirmReset bit = 0;
DECLARE @ResetScope nvarchar(20) = N'ALL';
DECLARE @ScenarioName nvarchar(120) = N'Base FMD SMT';

IF @ConfirmReset <> 1
BEGIN
    THROW 51000, 'Reset cancelado. Cambia @ConfirmReset a 1 para borrar datos.', 1;
END;

IF @ResetScope NOT IN (N'SMT_ONLY', N'ALL')
BEGIN
    THROW 51001, 'ResetScope invalido. Usa SMT_ONLY o ALL.', 1;
END;

BEGIN TRANSACTION;

IF @ResetScope = N'SMT_ONLY'
BEGIN
    DECLARE @ScenarioId int =
    (
        SELECT ScenarioId
        FROM planning.Scenarios
        WHERE ScenarioName = @ScenarioName
    );

    DECLARE @SmtOperationId int =
    (
        SELECT OperationId
        FROM planning.Operations
        WHERE OperationCode = N'SMT'
    );

    IF @ScenarioId IS NOT NULL
    BEGIN
        IF OBJECT_ID(N'planning.SmtCapacityTargets', N'U') IS NOT NULL
            DELETE FROM planning.SmtCapacityTargets WHERE ScenarioId = @ScenarioId;
        IF OBJECT_ID(N'planning.SmtLineAssignments', N'U') IS NOT NULL
            DELETE FROM planning.SmtLineAssignments WHERE ScenarioId = @ScenarioId;
        IF OBJECT_ID(N'planning.SmtFamilyVolumes', N'U') IS NOT NULL
            DELETE FROM planning.SmtFamilyVolumes WHERE ScenarioId = @ScenarioId;
        IF OBJECT_ID(N'planning.WeeklyDemand', N'U') IS NOT NULL
            DELETE FROM planning.WeeklyDemand WHERE ScenarioId = @ScenarioId;
        IF OBJECT_ID(N'planning.OperationCapacityResults', N'U') IS NOT NULL
            DELETE FROM planning.OperationCapacityResults WHERE ScenarioId = @ScenarioId;
        IF OBJECT_ID(N'planning.CapacityResults', N'U') IS NOT NULL
            DELETE FROM planning.CapacityResults WHERE ScenarioId = @ScenarioId;
        IF OBJECT_ID(N'planning.CalculationRuns', N'U') IS NOT NULL
            DELETE FROM planning.CalculationRuns WHERE ScenarioId = @ScenarioId;
        IF OBJECT_ID(N'planning.OperationWeeklyCapacities', N'U') IS NOT NULL
            DELETE FROM planning.OperationWeeklyCapacities WHERE ScenarioId = @ScenarioId;
        IF OBJECT_ID(N'planning.ResourceInventories', N'U') IS NOT NULL
            DELETE FROM planning.ResourceInventories WHERE ScenarioId = @ScenarioId;
        IF OBJECT_ID(N'planning.ScenarioRackInventories', N'U') IS NOT NULL
            DELETE FROM planning.ScenarioRackInventories WHERE ScenarioId = @ScenarioId;
        IF OBJECT_ID(N'planning.ScenarioMachinePartOverrides', N'U') IS NOT NULL
            DELETE FROM planning.ScenarioMachinePartOverrides WHERE ScenarioId = @ScenarioId;
        IF OBJECT_ID(N'planning.ScenarioMachineOverrides', N'U') IS NOT NULL
            DELETE FROM planning.ScenarioMachineOverrides WHERE ScenarioId = @ScenarioId;
        IF OBJECT_ID(N'planning.ScenarioPartOverrides', N'U') IS NOT NULL
            DELETE FROM planning.ScenarioPartOverrides WHERE ScenarioId = @ScenarioId;
        IF OBJECT_ID(N'planning.ProductionWeeks', N'U') IS NOT NULL
            DELETE FROM planning.ProductionWeeks WHERE ScenarioId = @ScenarioId;
        IF OBJECT_ID(N'planning.Scenarios', N'U') IS NOT NULL
            DELETE FROM planning.Scenarios WHERE ScenarioId = @ScenarioId;
    END;

    IF @SmtOperationId IS NOT NULL
    BEGIN
        IF OBJECT_ID(N'planning.SmtLineFamilyRates', N'U') IS NOT NULL
            DELETE FROM planning.SmtLineFamilyRates WHERE OperationId = @SmtOperationId;
        IF OBJECT_ID(N'planning.SmtFamilyRates', N'U') IS NOT NULL
            DELETE FROM planning.SmtFamilyRates WHERE OperationId = @SmtOperationId;
    END;
END;
ELSE
BEGIN
    IF OBJECT_ID(N'planning.SmtCapacityTargets', N'U') IS NOT NULL DELETE FROM planning.SmtCapacityTargets;
    IF OBJECT_ID(N'planning.SmtLineAssignments', N'U') IS NOT NULL DELETE FROM planning.SmtLineAssignments;
    IF OBJECT_ID(N'planning.SmtFamilyVolumes', N'U') IS NOT NULL DELETE FROM planning.SmtFamilyVolumes;
    IF OBJECT_ID(N'planning.SmtLineFamilyRates', N'U') IS NOT NULL DELETE FROM planning.SmtLineFamilyRates;
    IF OBJECT_ID(N'planning.SmtFamilyRates', N'U') IS NOT NULL DELETE FROM planning.SmtFamilyRates;
    IF OBJECT_ID(N'planning.OperationCapacityResults', N'U') IS NOT NULL DELETE FROM planning.OperationCapacityResults;
    IF OBJECT_ID(N'planning.CapacityResults', N'U') IS NOT NULL DELETE FROM planning.CapacityResults;
    IF OBJECT_ID(N'planning.CalculationRuns', N'U') IS NOT NULL DELETE FROM planning.CalculationRuns;
    IF OBJECT_ID(N'planning.ResourceInventories', N'U') IS NOT NULL DELETE FROM planning.ResourceInventories;
    IF OBJECT_ID(N'planning.ScenarioRackInventories', N'U') IS NOT NULL DELETE FROM planning.ScenarioRackInventories;
    IF OBJECT_ID(N'planning.WeeklyDemand', N'U') IS NOT NULL DELETE FROM planning.WeeklyDemand;
    IF OBJECT_ID(N'planning.OperationWeeklyCapacities', N'U') IS NOT NULL DELETE FROM planning.OperationWeeklyCapacities;
    IF OBJECT_ID(N'planning.TestTimeDefinitions', N'U') IS NOT NULL DELETE FROM planning.TestTimeDefinitions;
    IF OBJECT_ID(N'planning.OperationRates', N'U') IS NOT NULL DELETE FROM planning.OperationRates;
    IF OBJECT_ID(N'planning.ResourceConfigurations', N'U') IS NOT NULL DELETE FROM planning.ResourceConfigurations;
    IF OBJECT_ID(N'planning.ResourceTypes', N'U') IS NOT NULL DELETE FROM planning.ResourceTypes;
    IF OBJECT_ID(N'planning.RackConfigurations', N'U') IS NOT NULL DELETE FROM planning.RackConfigurations;
    IF OBJECT_ID(N'planning.RackAccessories', N'U') IS NOT NULL DELETE FROM planning.RackAccessories;
    IF OBJECT_ID(N'planning.RackTypes', N'U') IS NOT NULL DELETE FROM planning.RackTypes;
    IF OBJECT_ID(N'planning.ScenarioMachinePartOverrides', N'U') IS NOT NULL DELETE FROM planning.ScenarioMachinePartOverrides;
    IF OBJECT_ID(N'planning.ScenarioMachineOverrides', N'U') IS NOT NULL DELETE FROM planning.ScenarioMachineOverrides;
    IF OBJECT_ID(N'planning.ScenarioPartOverrides', N'U') IS NOT NULL DELETE FROM planning.ScenarioPartOverrides;
    IF OBJECT_ID(N'planning.ProcessRoutes', N'U') IS NOT NULL DELETE FROM planning.ProcessRoutes;
    IF OBJECT_ID(N'planning.PartCrossReferences', N'U') IS NOT NULL DELETE FROM planning.PartCrossReferences;
    IF OBJECT_ID(N'planning.ProductionWeeks', N'U') IS NOT NULL DELETE FROM planning.ProductionWeeks;
    IF OBJECT_ID(N'planning.Operations', N'U') IS NOT NULL DELETE FROM planning.Operations;
    IF OBJECT_ID(N'planning.Machines', N'U') IS NOT NULL DELETE FROM planning.Machines;
    IF OBJECT_ID(N'planning.PartNumbers', N'U') IS NOT NULL DELETE FROM planning.PartNumbers;
    IF OBJECT_ID(N'planning.SubFamilies', N'U') IS NOT NULL DELETE FROM planning.SubFamilies;
    IF OBJECT_ID(N'planning.Scenarios', N'U') IS NOT NULL DELETE FROM planning.Scenarios;
END;

DECLARE @TableName sysname;
DECLARE reseed_cursor CURSOR LOCAL FAST_FORWARD FOR
SELECT name
FROM sys.tables
WHERE schema_id = SCHEMA_ID(N'planning')
    AND OBJECTPROPERTY(object_id, 'TableHasIdentity') = 1;

OPEN reseed_cursor;
FETCH NEXT FROM reseed_cursor INTO @TableName;

WHILE @@FETCH_STATUS = 0
BEGIN
    DECLARE @QualifiedTable nvarchar(300) = N'planning.' + QUOTENAME(@TableName);
    DECLARE @ReseedSql nvarchar(max) =
        N'DBCC CHECKIDENT (''' + REPLACE(@QualifiedTable, '''', '''''') + N''', RESEED, 0) WITH NO_INFOMSGS;';
    EXEC sys.sp_executesql @ReseedSql;
    FETCH NEXT FROM reseed_cursor INTO @TableName;
END;

CLOSE reseed_cursor;
DEALLOCATE reseed_cursor;

COMMIT TRANSACTION;

SELECT
    @ResetScope AS ResetScope,
    @ScenarioName AS ScenarioName,
    'Datos borrados correctamente' AS Resultado;
GO

SELECT 'Scenarios' AS Tabla, COUNT(*) AS Registros FROM planning.Scenarios
UNION ALL SELECT 'ProductionWeeks', COUNT(*) FROM planning.ProductionWeeks
UNION ALL SELECT 'PartNumbers', COUNT(*) FROM planning.PartNumbers
UNION ALL SELECT 'SubFamilies', COUNT(*) FROM planning.SubFamilies
UNION ALL SELECT 'Operations', COUNT(*) FROM planning.Operations
UNION ALL SELECT 'WeeklyDemand', COUNT(*) FROM planning.WeeklyDemand
UNION ALL SELECT 'SmtFamilyVolumes', COUNT(*) FROM planning.SmtFamilyVolumes
UNION ALL SELECT 'SmtFamilyRates', COUNT(*) FROM planning.SmtFamilyRates
UNION ALL SELECT 'SmtLineFamilyRates', COUNT(*) FROM planning.SmtLineFamilyRates
UNION ALL SELECT 'SmtLineAssignments', COUNT(*) FROM planning.SmtLineAssignments
UNION ALL SELECT 'SmtCapacityTargets', COUNT(*) FROM planning.SmtCapacityTargets;
GO
