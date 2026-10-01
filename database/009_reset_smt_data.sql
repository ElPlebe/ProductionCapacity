/*
    ProductionCapacity - Reset solo SMT

    Uso:
    1. Ejecuta este script dentro de la base ProductionCapacity.
    2. Cambia @ConfirmReset a 1.
    3. Deja @ScenarioName = N'Base FMD SMT' si vas a limpiar el escenario importado desde FMD SMT.

    Alcance:
    - Borra resultados, semanas, demanda y asignaciones del escenario SMT.
    - Borra rates SMT por familia y por linea.
    - Borra operation rates asociados a la operacion SMT.
    - Conserva las tablas, catalogos base y la operacion SMT para poder reimportar.

    Nota:
    - No borra PartNumbers ni CrossReferences, porque pueden usarse en otros modulos.
*/

SET XACT_ABORT ON;
GO

DECLARE @ConfirmReset bit = 0;
DECLARE @ScenarioName nvarchar(120) = N'Base FMD SMT';

IF @ConfirmReset <> 1
BEGIN
    THROW 51000, 'Reset SMT cancelado. Cambia @ConfirmReset a 1 para borrar datos SMT.', 1;
END;

BEGIN TRANSACTION;

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

    IF OBJECT_ID(N'planning.OperationRates', N'U') IS NOT NULL
        DELETE FROM planning.OperationRates WHERE OperationId = @SmtOperationId;
END;

COMMIT TRANSACTION;

SELECT
    @ScenarioName AS ScenarioName,
    N'Datos SMT borrados correctamente' AS Resultado;
GO

SELECT 'Scenarios' AS Tabla, COUNT(*) AS Registros FROM planning.Scenarios WHERE ScenarioName = N'Base FMD SMT'
UNION ALL SELECT 'ProductionWeeks Base FMD SMT', COUNT(*)
FROM planning.ProductionWeeks pw
INNER JOIN planning.Scenarios s ON s.ScenarioId = pw.ScenarioId
WHERE s.ScenarioName = N'Base FMD SMT'
UNION ALL SELECT 'WeeklyDemand Base FMD SMT', COUNT(*)
FROM planning.WeeklyDemand wd
INNER JOIN planning.Scenarios s ON s.ScenarioId = wd.ScenarioId
WHERE s.ScenarioName = N'Base FMD SMT'
UNION ALL SELECT 'SmtFamilyVolumes', COUNT(*) FROM planning.SmtFamilyVolumes
UNION ALL SELECT 'SmtFamilyRates', COUNT(*) FROM planning.SmtFamilyRates
UNION ALL SELECT 'SmtLineFamilyRates', COUNT(*) FROM planning.SmtLineFamilyRates
UNION ALL SELECT 'SmtLineAssignments', COUNT(*) FROM planning.SmtLineAssignments
UNION ALL SELECT 'SmtCapacityTargets', COUNT(*) FROM planning.SmtCapacityTargets;
GO
