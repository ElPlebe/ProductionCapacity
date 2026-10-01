using Microsoft.EntityFrameworkCore;
using ProductionCapacity.Data.Entities;

namespace ProductionCapacity.Data;

public sealed record SmtCapacityParityResult(
    string ScenarioName,
    decimal Tolerance,
    IReadOnlyList<SmtCapacityParityRow> Rows)
{
    public bool IsPassing => Rows.Count > 0 && Rows.All(row => row.IsPassing);
    public int DifferenceCount => Rows.Count(row => !row.IsPassing);
    public int TargetCount => Rows.Count(row => row.ExpectedAssignedDemandUnits is not null);
}

public sealed record SmtCapacityParityRow(
    string WeekName,
    string OperationCode,
    string LineCode,
    string ProductFamily,
    decimal? ExpectedAssignedDemandUnits,
    decimal? ActualAssignedDemandUnits,
    decimal? ExpectedMaxCapacityUnits,
    decimal? ActualMaxCapacityUnits,
    decimal? ExpectedUtilizationPercent,
    decimal? ActualUtilizationPercent,
    decimal? ExpectedDeltaCapacityUnits,
    decimal? ActualDeltaCapacityUnits,
    string? SourceReference,
    string? Message,
    bool IsPassing);

public sealed class SmtCapacityParityService(
    PlanningDbContext db,
    SmtCapacityReportService reportService)
{
    public async Task<SmtCapacityParityResult> CompareAsync(
        string scenarioName,
        decimal tolerance = 0.05m,
        CancellationToken cancellationToken = default)
    {
        var report = await reportService.GetReportAsync(scenarioName, cancellationToken);
        var scenario = await db.Scenarios
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.ScenarioName == scenarioName, cancellationToken)
            ?? throw new InvalidOperationException($"Scenario '{scenarioName}' was not found.");

        var targets = await (
            from target in db.SmtCapacityTargets.AsNoTracking()
            join week in db.ProductionWeeks.AsNoTracking()
                on target.ProductionWeekId equals week.ProductionWeekId
            join operation in db.Operations.AsNoTracking()
                on target.OperationId equals operation.OperationId
            where target.ScenarioId == scenario.ScenarioId
            select new SmtTargetProjection(target, week.WeekName, operation.OperationCode))
            .ToListAsync(cancellationToken);

        var rows = new List<SmtCapacityParityRow>();
        var actualByKey = report.Rows.ToDictionary(
            row => BuildKey(row.WeekName, row.OperationCode, row.LineCode, row.ProductFamily),
            StringComparer.OrdinalIgnoreCase);
        var targetKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var target in targets)
        {
            var key = BuildKey(target.WeekName, target.OperationCode, target.Target.LineCode, target.Target.ProductFamily);
            targetKeys.Add(key);
            if (!actualByKey.TryGetValue(key, out var actual))
            {
                rows.Add(new SmtCapacityParityRow(
                    target.WeekName,
                    target.OperationCode,
                    target.Target.LineCode,
                    target.Target.ProductFamily,
                    target.Target.ExpectedAssignedDemandUnits,
                    null,
                    target.Target.ExpectedMaxCapacityUnits,
                    null,
                    target.Target.ExpectedUtilizationPercent,
                    null,
                    target.Target.ExpectedDeltaCapacityUnits,
                    null,
                    target.Target.SourceReference,
                    "No existe fila calculada por el sistema para este target de Capacity SMT.",
                    false));
                continue;
            }

            var passes =
                IsWithinTolerance(target.Target.ExpectedAssignedDemandUnits, actual.AssignedDemandUnits, tolerance) &&
                IsWithinTolerance(target.Target.ExpectedMaxCapacityUnits, actual.MaxCapacityUnits, tolerance) &&
                IsWithinTolerance(target.Target.ExpectedUtilizationPercent, actual.UtilizationPercent, tolerance) &&
                IsWithinTolerance(target.Target.ExpectedDeltaCapacityUnits, actual.DeltaCapacityUnits, tolerance);

            rows.Add(new SmtCapacityParityRow(
                actual.WeekName,
                actual.OperationCode,
                actual.LineCode,
                actual.ProductFamily,
                target.Target.ExpectedAssignedDemandUnits,
                actual.AssignedDemandUnits,
                target.Target.ExpectedMaxCapacityUnits,
                actual.MaxCapacityUnits,
                target.Target.ExpectedUtilizationPercent,
                actual.UtilizationPercent,
                target.Target.ExpectedDeltaCapacityUnits,
                actual.DeltaCapacityUnits,
                target.Target.SourceReference,
                passes ? null : "Diferencia contra Capacity SMT fuera de tolerancia.",
                passes));
        }

        foreach (var actual in report.Rows)
        {
            var key = BuildKey(actual.WeekName, actual.OperationCode, actual.LineCode, actual.ProductFamily);
            if (targetKeys.Contains(key))
            {
                continue;
            }

            rows.Add(new SmtCapacityParityRow(
                actual.WeekName,
                actual.OperationCode,
                actual.LineCode,
                actual.ProductFamily,
                null,
                actual.AssignedDemandUnits,
                null,
                actual.MaxCapacityUnits,
                null,
                actual.UtilizationPercent,
                null,
                actual.DeltaCapacityUnits,
                null,
                "No hay target Excel cargado para esta fila calculada.",
                false));
        }

        return new SmtCapacityParityResult(
            scenarioName,
            tolerance,
            rows
                .OrderBy(row => row.WeekName)
                .ThenBy(row => row.LineCode)
                .ThenBy(row => row.ProductFamily)
                .ToList());
    }

    private static string BuildKey(string weekName, string operationCode, string lineCode, string productFamily) =>
        $"{weekName}|{operationCode}|{lineCode}|{productFamily}";

    private static bool IsWithinTolerance(decimal expected, decimal actual, decimal tolerance) =>
        Math.Abs(expected - actual) <= tolerance;

    private sealed record SmtTargetProjection(
        SmtCapacityTargetEntity Target,
        string WeekName,
        string OperationCode);
}
