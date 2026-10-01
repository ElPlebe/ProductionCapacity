using Microsoft.EntityFrameworkCore;

namespace ProductionCapacity.Data;

public sealed record SmtSummaryReport(
    string ScenarioName,
    IReadOnlyList<SmtSummaryMonthColumn> Months,
    IReadOnlyList<SmtLinearityRow> LinearityRows,
    IReadOnlyList<SmtBuildProjectionRow> BuildProjectionRows,
    IReadOnlyList<SmtScrCommitRow> ScrCommitRows,
    IReadOnlyList<SmtSummaryCapacityRow> CapacityRows,
    IReadOnlyList<SmtSummaryBottleneckRow> BottleneckRows);

public sealed record SmtSummaryMonthColumn(
    string WeekName,
    string? MonthName,
    int MonthSequence,
    int WeekCount);

public sealed record SmtLinearityRow(
    string BusinessUnit,
    IReadOnlyDictionary<string, decimal> MonthPercents);

public sealed record SmtBuildProjectionRow(
    string BusinessUnit,
    string FamilyLabel,
    decimal TotalMps,
    IReadOnlyDictionary<string, decimal> MonthlyMps);

public sealed record SmtScrCommitRow(
    string BusinessUnit,
    decimal TotalCommit,
    IReadOnlyDictionary<string, decimal> MonthlyCommit);

public sealed record SmtSummaryCapacityRow(
    string LineCode,
    IReadOnlyDictionary<string, SmtSummaryCapacityCell> Months);

public sealed record SmtSummaryCapacityCell(
    decimal CapacityUnits,
    decimal UtilizationPercent,
    decimal DeltaCapacityUnits);

public sealed record SmtSummaryBottleneckRow(
    string WeekName,
    string? MonthName,
    string LineCode,
    decimal UtilizationPercent,
    decimal CapacityUnits,
    decimal DeltaCapacityUnits);

public sealed class SmtSummaryService(
    PlanningDbContext db,
    SmtCapacityReportService capacityReportService)
{
    public async Task<SmtSummaryReport> GetSummaryAsync(
        string scenarioName,
        CancellationToken cancellationToken = default)
    {
        var report = await capacityReportService.GetReportAsync(scenarioName, cancellationToken);
        var scenario = await db.Scenarios
            .AsNoTracking()
            .SingleAsync(item => item.ScenarioName == scenarioName, cancellationToken);
        var volumes = await db.SmtFamilyVolumes
            .AsNoTracking()
            .Where(item => item.ScenarioId == scenario.ScenarioId)
            .ToListAsync(cancellationToken);
        var months = BuildMonths(report, volumes);
        var buildProjectionRows = BuildProjection(volumes, months);
        var linearityRows = BuildLinearity(buildProjectionRows, months);
        var capacityRows = BuildCapacityRows(report, months);
        var bottleneckRows = BuildBottleneckRows(report, months);
        var scrCommitRows = await BuildScrCommitRowsAsync(scenario.ScenarioId, months, cancellationToken);

        return new SmtSummaryReport(
            report.ScenarioName,
            months,
            linearityRows,
            buildProjectionRows,
            scrCommitRows,
            capacityRows,
            bottleneckRows);
    }

    private static IReadOnlyList<SmtSummaryMonthColumn> BuildMonths(
        SmtCapacityReport report,
        IReadOnlyList<Entities.SmtFamilyVolumeEntity> volumes)
    {
        if (report.WeekColumns.Count > 0)
        {
            return report.WeekColumns
                .Select(item => new SmtSummaryMonthColumn(item.WeekName, item.MonthName, item.MonthSequence, item.WeekCount))
                .ToList();
        }

        return volumes
            .GroupBy(item => new { item.MonthSequence, item.MonthName })
            .OrderBy(group => group.Key.MonthSequence)
            .Select(group => new SmtSummaryMonthColumn(
                $"M{group.Key.MonthSequence}",
                group.Key.MonthName,
                group.Key.MonthSequence,
                group.Key.MonthSequence == 3 ? 5 : 4))
            .ToList();
    }

    private static IReadOnlyList<SmtBuildProjectionRow> BuildProjection(
        IReadOnlyList<Entities.SmtFamilyVolumeEntity> volumes,
        IReadOnlyList<SmtSummaryMonthColumn> months)
    {
        var monthBySequence = months.ToDictionary(item => item.MonthSequence, item => item.WeekName);
        return volumes
            .GroupBy(item => item.BusinessUnit ?? "Sin BU", StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var monthly = months.ToDictionary(month => month.WeekName, _ => 0m, StringComparer.OrdinalIgnoreCase);
                foreach (var volumeGroup in group.GroupBy(item => item.MonthSequence))
                {
                    if (monthBySequence.TryGetValue(volumeGroup.Key, out var weekName))
                    {
                        monthly[weekName] = volumeGroup.Sum(item => item.MonthlyMps);
                    }
                }

                return new SmtBuildProjectionRow(
                    group.Key,
                    group.Key,
                    monthly.Values.Sum(),
                    monthly);
            })
            .ToList();
    }

    private static IReadOnlyList<SmtLinearityRow> BuildLinearity(
        IReadOnlyList<SmtBuildProjectionRow> buildProjectionRows,
        IReadOnlyList<SmtSummaryMonthColumn> months) =>
        buildProjectionRows
            .Select(row =>
            {
                var total = row.TotalMps;
                var percentages = months.ToDictionary(
                    month => month.WeekName,
                    month => total <= 0m ? 0m : (row.MonthlyMps.GetValueOrDefault(month.WeekName) / total) * 100m,
                    StringComparer.OrdinalIgnoreCase);
                return new SmtLinearityRow(row.BusinessUnit, percentages);
            })
            .ToList();

    private static IReadOnlyList<SmtSummaryCapacityRow> BuildCapacityRows(
        SmtCapacityReport report,
        IReadOnlyList<SmtSummaryMonthColumn> months) =>
        report.LineSummaries
            .GroupBy(item => item.LineCode)
            .OrderBy(group => ParseLineNumber(group.Key))
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var cells = months.ToDictionary(
                    month => month.WeekName,
                    month =>
                    {
                        var summary = group.FirstOrDefault(item => item.WeekName == month.WeekName);
                        return summary is null
                            ? new SmtSummaryCapacityCell(0m, 0m, 0m)
                            : new SmtSummaryCapacityCell(summary.MaxCapacityUnits, summary.UtilizationPercent, summary.DeltaCapacityUnits);
                    },
                    StringComparer.OrdinalIgnoreCase);
                return new SmtSummaryCapacityRow(group.Key, cells);
            })
            .ToList();

    private static IReadOnlyList<SmtSummaryBottleneckRow> BuildBottleneckRows(
        SmtCapacityReport report,
        IReadOnlyList<SmtSummaryMonthColumn> months) =>
        months
            .Select(month =>
            {
                var bottleneck = report.LineSummaries
                    .Where(item => item.WeekName == month.WeekName)
                    .OrderByDescending(item => item.UtilizationPercent)
                    .ThenBy(item => item.LineCode, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault();

                return bottleneck is null
                    ? new SmtSummaryBottleneckRow(month.WeekName, month.MonthName, "-", 0m, 0m, 0m)
                    : new SmtSummaryBottleneckRow(
                        month.WeekName,
                        month.MonthName,
                        bottleneck.LineCode,
                        bottleneck.UtilizationPercent,
                        bottleneck.MaxCapacityUnits,
                        bottleneck.DeltaCapacityUnits);
            })
            .ToList();

    private async Task<IReadOnlyList<SmtScrCommitRow>> BuildScrCommitRowsAsync(
        int scenarioId,
        IReadOnlyList<SmtSummaryMonthColumn> months,
        CancellationToken cancellationToken)
    {
        var targets = await (
            from target in db.SmtCapacityTargets.AsNoTracking()
            join week in db.ProductionWeeks.AsNoTracking()
                on target.ProductionWeekId equals week.ProductionWeekId
            where target.ScenarioId == scenarioId
            select new
            {
                target.ProductFamily,
                week.WeekName,
                target.ExpectedAssignedDemandUnits
            })
            .ToListAsync(cancellationToken);
        if (targets.Count == 0)
        {
            return [];
        }

        var businessUnitsByFamily = await db.SmtFamilyVolumes
            .AsNoTracking()
            .Where(item => item.ScenarioId == scenarioId)
            .GroupBy(item => item.ProductFamily)
            .Select(group => new
            {
                ProductFamily = group.Key,
                BusinessUnit = group.Select(item => item.BusinessUnit).FirstOrDefault(item => item != null) ?? "Sin BU"
            })
            .ToListAsync(cancellationToken);
        var businessUnitLookup = businessUnitsByFamily.ToDictionary(
            item => item.ProductFamily,
            item => item.BusinessUnit,
            StringComparer.OrdinalIgnoreCase);

        return targets
            .GroupBy(item => businessUnitLookup.GetValueOrDefault(item.ProductFamily, "Sin BU"), StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var monthly = months.ToDictionary(
                    month => month.WeekName,
                    month => group
                        .Where(item => item.WeekName == month.WeekName)
                        .Sum(item => item.ExpectedAssignedDemandUnits),
                    StringComparer.OrdinalIgnoreCase);
                return new SmtScrCommitRow(group.Key, monthly.Values.Sum(), monthly);
            })
            .ToList();
    }

    private static int ParseLineNumber(string lineCode)
    {
        var digits = new string(lineCode.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var lineNumber) ? lineNumber : 0;
    }
}
