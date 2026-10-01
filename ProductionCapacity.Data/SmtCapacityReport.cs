using Microsoft.EntityFrameworkCore;
using ProductionCapacity.Data.Entities;
using ProductionCapacity.Engine;

namespace ProductionCapacity.Data;

public sealed record SmtCapacityReport(
    string ScenarioName,
    IReadOnlyList<SmtCapacityReportRow> Rows,
    IReadOnlyList<SmtCapacityLineSummary> LineSummaries,
    IReadOnlyList<SmtCapacityWeekColumn> WeekColumns,
    IReadOnlyList<SmtCapacityLineBlock> LineBlocks);

public sealed record SmtCapacityWeekColumn(
    string WeekName,
    string? MonthName,
    int MonthSequence,
    int WeekCount);

public sealed record SmtCapacityLineBlock(
    string LineCode,
    int LineNumber,
    IReadOnlyList<SmtCapacityFamilyLineRow> FamilyRows,
    IReadOnlyDictionary<string, SmtCapacityLineSummary> WeeklySummaries);

public sealed record SmtCapacityFamilyLineRow(
    string OperationCode,
    string LineCode,
    int LineNumber,
    string ProductFamily,
    string? SubFamily,
    string? BusinessUnit,
    decimal LineDistribution,
    decimal UnitsPerHour,
    decimal ChangeoverCount,
    decimal ChangeoverHoursMonth,
    decimal AvailableHours,
    IReadOnlyDictionary<string, SmtCapacityWeekCell> WeekCells);

public sealed record SmtCapacityWeekCell(
    decimal WeeklyDemandUnits,
    decimal MonthlyMps,
    decimal AssignedDemandUnits,
    decimal MaxCapacityUnits,
    decimal UtilizationPercent,
    decimal DeltaCapacityUnits);

public sealed record SmtCapacityReportRow(
    string WeekName,
    string? MonthName,
    int MonthSequence,
    string OperationCode,
    string LineCode,
    int LineNumber,
    string ProductFamily,
    string? SubFamily,
    string? BusinessUnit,
    decimal LineDistribution,
    decimal WeeklyDemandUnits,
    decimal MonthlyMps,
    decimal AssignedDemandUnits,
    decimal UnitsPerHour,
    decimal ChangeoverCount,
    decimal ChangeoverHoursMonth,
    decimal AvailableHours,
    decimal MaxCapacityUnits,
    decimal UtilizationPercent,
    decimal DeltaCapacityUnits);

public sealed record SmtCapacityLineSummary(
    string WeekName,
    string LineCode,
    decimal AssignedDemandUnits,
    decimal AvailableHours,
    decimal MaxCapacityUnits,
    decimal UtilizationPercent,
    decimal DeltaCapacityUnits);

public sealed record SmtCapacityAssignmentEdit(
    string ScenarioName,
    string WeekName,
    string OperationCode,
    string ProductFamily,
    string CurrentLineCode,
    string NewLineCode,
    decimal LineDistributionPercent);

internal sealed record SmtCapacityDraftRow(
    string WeekName,
    string? MonthName,
    int MonthSequence,
    string OperationCode,
    string LineCode,
    int LineNumber,
    string ProductFamily,
    string? SubFamily,
    string? BusinessUnit,
    decimal LineDistribution,
    decimal WeeklyDemandUnits,
    decimal MonthlyMps,
    decimal AssignedDemandUnits,
    decimal UnitsPerHour,
    decimal ChangeoverCount,
    decimal ChangeoverHoursMonth,
    decimal AvailableHours,
    decimal BaseMonthlyCapacityUnits,
    decimal RawUtilizationPercent);

public sealed class SmtCapacityReportService(PlanningDbContext db)
{
    private const decimal ModelChangeHours = 4m;
    private const decimal TargetLineUtilizationPercent = 85m;

    public async Task<SmtCapacityReport> GetReportAsync(
        string scenarioName,
        CancellationToken cancellationToken = default)
    {
        var scenario = await db.Scenarios
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.ScenarioName == scenarioName, cancellationToken)
            ?? throw new InvalidOperationException($"Scenario '{scenarioName}' was not found.");

        var assignments = await (
            from assignment in db.SmtLineAssignments.AsNoTracking()
            join week in db.ProductionWeeks.AsNoTracking()
                on assignment.ProductionWeekId equals week.ProductionWeekId
            join operation in db.Operations.AsNoTracking()
                on assignment.OperationId equals operation.OperationId
            where assignment.ScenarioId == scenario.ScenarioId && assignment.IsActive
            select new
            {
                Assignment = assignment,
                Week = week,
                Operation = operation
            })
            .OrderBy(item => item.Week.ProductionWeekId)
            .ThenBy(item => item.Assignment.LineNumber)
            .ThenBy(item => item.Assignment.ProductFamily)
            .ToListAsync(cancellationToken);
        var subFamiliesByFamily = await GetSubFamiliesByFamilyAsync(cancellationToken);

        var drafts = new List<SmtCapacityDraftRow>();
        foreach (var item in assignments)
        {
            var capacity = await db.OperationWeeklyCapacities
                .AsNoTracking()
                .SingleOrDefaultAsync(capacity =>
                    capacity.ScenarioId == scenario.ScenarioId &&
                    capacity.ProductionWeekId == item.Week.ProductionWeekId &&
                    capacity.OperationId == item.Operation.OperationId,
                    cancellationToken);
            var rate = await db.SmtFamilyRates
                .AsNoTracking()
                .SingleOrDefaultAsync(rate =>
                    rate.OperationId == item.Operation.OperationId &&
                    rate.ProductFamily == item.Assignment.ProductFamily &&
                    rate.IsActive,
                    cancellationToken);
            var lineRate = await db.SmtLineFamilyRates
                .AsNoTracking()
                .SingleOrDefaultAsync(rate =>
                    rate.OperationId == item.Operation.OperationId &&
                    rate.ProductFamily == item.Assignment.ProductFamily &&
                    rate.LineCode == item.Assignment.LineCode &&
                    rate.IsActive,
                    cancellationToken);
            var volume = await db.SmtFamilyVolumes
                .AsNoTracking()
                .SingleOrDefaultAsync(volume =>
                    volume.ScenarioId == scenario.ScenarioId &&
                    volume.ProductionWeekId == item.Week.ProductionWeekId &&
                    volume.ProductFamily == item.Assignment.ProductFamily,
                    cancellationToken);

            if (capacity is null || rate is null)
            {
                continue;
            }

            var weeklyDemand = volume?.WeeklyMps > 0m
                ? volume.WeeklyMps
                : await GetWeeklyDemandAsync(scenario.ScenarioId, item.Week.ProductionWeekId, item.Assignment.ProductFamily, cancellationToken);
            var monthlyDemand = volume?.MonthlyMps > 0m
                ? volume.MonthlyMps
                : weeklyDemand * GetWeekCount(volume?.MonthSequence > 0 ? volume.MonthSequence : item.Assignment.MonthSequence);
            var assignedDemand = monthlyDemand * item.Assignment.LineDistribution;
            var unitsPerHour = lineRate?.UnitsPerHour > 0m ? lineRate.UnitsPerHour : rate.UnitsPerHour;
            var grossAvailableHours = GetSmtMonthAvailableHours(capacity, volume?.MonthSequence > 0 ? volume.MonthSequence : item.Assignment.MonthSequence);
            var changeoverHours = rate.ChangeoverCount * ModelChangeHours;
            var availableHours = Math.Max(0m, grossAvailableHours - changeoverHours);
            var baseMonthlyCapacity = unitsPerHour * availableHours;
            var rawUtilizationPercent = baseMonthlyCapacity == 0m
                ? 0m
                : (monthlyDemand / baseMonthlyCapacity) * item.Assignment.LineDistribution * 100m;

            drafts.Add(new SmtCapacityDraftRow(
                item.Week.WeekName,
                volume?.MonthName ?? item.Assignment.MonthName,
                volume?.MonthSequence > 0 ? volume.MonthSequence : item.Assignment.MonthSequence,
                item.Operation.OperationCode,
                item.Assignment.LineCode,
                item.Assignment.LineNumber,
                item.Assignment.ProductFamily,
                subFamiliesByFamily.GetValueOrDefault(item.Assignment.ProductFamily),
                volume?.BusinessUnit,
                item.Assignment.LineDistribution,
                weeklyDemand,
                monthlyDemand,
                assignedDemand,
                unitsPerHour,
                rate.ChangeoverCount,
                changeoverHours,
                availableHours,
                baseMonthlyCapacity,
                rawUtilizationPercent));
        }

        var rows = drafts
            .GroupBy(row => new { row.WeekName, row.LineCode })
            .SelectMany(group =>
            {
                var lineUtilizationPercent = group.Sum(row => row.RawUtilizationPercent);
                var lineFactor = lineUtilizationPercent <= 0m
                    ? 0m
                    : TargetLineUtilizationPercent / lineUtilizationPercent;

                return group.Select(row =>
                {
                    var maxCapacityUnits = row.AssignedDemandUnits * lineFactor;
                    return new SmtCapacityReportRow(
                        row.WeekName,
                        row.MonthName,
                        row.MonthSequence,
                        row.OperationCode,
                        row.LineCode,
                        row.LineNumber,
                        row.ProductFamily,
                        row.SubFamily,
                        row.BusinessUnit,
                        Round(row.LineDistribution),
                        Round(row.WeeklyDemandUnits),
                        Round(row.MonthlyMps),
                        Round(row.AssignedDemandUnits),
                        Round(row.UnitsPerHour),
                        Round(row.ChangeoverCount),
                        Round(row.ChangeoverHoursMonth),
                        Round(row.AvailableHours),
                        Round(maxCapacityUnits),
                        Round(row.RawUtilizationPercent),
                        Round(maxCapacityUnits - row.MonthlyMps));
                });
            })
            .ToList();

        var summaries = rows
            .GroupBy(row => new { row.WeekName, row.LineCode })
            .Select(group =>
            {
                var assignedDemand = group.Sum(row => row.AssignedDemandUnits);
                var availableHours = group.Max(row => row.AvailableHours);
                var maxCapacityUnits = group.Sum(row => row.MaxCapacityUnits);
                var utilizationPercent = group.Sum(row => row.UtilizationPercent);
                return new SmtCapacityLineSummary(
                    group.Key.WeekName,
                    group.Key.LineCode,
                    Round(assignedDemand),
                    Round(availableHours),
                    Round(maxCapacityUnits),
                    Round(utilizationPercent),
                    Round(maxCapacityUnits - assignedDemand));
            })
            .OrderBy(item => item.WeekName)
            .ThenBy(item => item.LineCode)
            .ToList();

        var weekColumns = BuildWeekColumns(rows);
        var lineBlocks = BuildLineBlocks(rows, summaries);

        return new SmtCapacityReport(scenario.ScenarioName, rows, summaries, weekColumns, lineBlocks);
    }

    public async Task UpdateLineAssignmentAsync(
        SmtCapacityAssignmentEdit edit,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(edit);
        if (edit.LineDistributionPercent <= 0m)
        {
            throw new InvalidOperationException("El factor debe ser mayor a 0%.");
        }

        if (edit.LineDistributionPercent > 100m)
        {
            throw new InvalidOperationException("El factor no puede ser mayor a 100%.");
        }

        var scenarioName = Required(edit.ScenarioName);
        var weekName = Required(edit.WeekName);
        var operationCode = Required(edit.OperationCode).ToUpperInvariant();
        var productFamily = Required(edit.ProductFamily).ToUpperInvariant();
        var currentLineCode = Required(edit.CurrentLineCode).ToUpperInvariant();
        var newLineCode = Required(edit.NewLineCode).ToUpperInvariant();

        var assignment = await (
            from existing in db.SmtLineAssignments
            join scenario in db.Scenarios on existing.ScenarioId equals scenario.ScenarioId
            join week in db.ProductionWeeks on existing.ProductionWeekId equals week.ProductionWeekId
            join operation in db.Operations on existing.OperationId equals operation.OperationId
            where scenario.ScenarioName == scenarioName &&
                week.WeekName == weekName &&
                operation.OperationCode == operationCode &&
                existing.ProductFamily == productFamily &&
                existing.LineCode == currentLineCode &&
                existing.IsActive
            select existing)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("No se encontro la asignacion SMT seleccionada.");

        var targetExists = await db.SmtLineAssignments.AnyAsync(existing =>
            existing.SmtLineAssignmentId != assignment.SmtLineAssignmentId &&
            existing.ScenarioId == assignment.ScenarioId &&
            existing.ProductionWeekId == assignment.ProductionWeekId &&
            existing.OperationId == assignment.OperationId &&
            existing.ProductFamily == productFamily &&
            existing.LineCode == newLineCode &&
            existing.IsActive,
            cancellationToken);
        if (targetExists)
        {
            throw new InvalidOperationException($"Ya existe una asignacion activa de {productFamily} en {newLineCode} para {weekName}.");
        }

        assignment.LineCode = newLineCode;
        assignment.LineNumber = ParseLineNumber(newLineCode);
        assignment.LineDistribution = edit.LineDistributionPercent / 100m;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<decimal> GetWeeklyDemandAsync(
        int scenarioId,
        int productionWeekId,
        string productFamily,
        CancellationToken cancellationToken)
    {
        return await (
            from demand in db.WeeklyDemand.AsNoTracking()
            join part in db.PartNumbers.AsNoTracking()
                on demand.PartNumberId equals part.PartNumberId
            where demand.ScenarioId == scenarioId &&
                demand.ProductionWeekId == productionWeekId &&
                part.ProductFamily == productFamily
            select demand.DemandUnits)
            .SumAsync(cancellationToken);
    }

    private async Task<IReadOnlyDictionary<string, string>> GetSubFamiliesByFamilyAsync(CancellationToken cancellationToken)
    {
        var rows = await db.PartNumbers
            .AsNoTracking()
            .Where(part => part.IsActive && part.ProductFamily != null && part.SubFamily != null)
            .Select(part => new { ProductFamily = part.ProductFamily!, SubFamily = part.SubFamily! })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => row.ProductFamily, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => string.Join(", ", group
                    .Select(row => row.SubFamily)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(subFamily => subFamily, StringComparer.OrdinalIgnoreCase)),
                StringComparer.OrdinalIgnoreCase);
    }

    private static decimal Round(decimal value) =>
        DecimalRounding.RoundUp(value);

    private static string Required(string value) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("El campo es requerido.") : value.Trim();

    private static int ParseLineNumber(string lineCode)
    {
        var digits = new string(lineCode.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var lineNumber) && lineNumber > 0 ? lineNumber : 1;
    }

    private static int GetWeekCount(int monthSequence) =>
        monthSequence == 3 ? 5 : 4;

    private static decimal GetSmtMonthAvailableHours(OperationWeeklyCapacityEntity capacity, int monthSequence)
    {
        var workDays = Math.Max(0m, capacity.WorkDays - capacity.HolidayDays);
        var weekCount = GetWeekCount(monthSequence);
        return (workDays * capacity.HoursPerShift * capacity.ShiftsPerDay * weekCount)
            + capacity.OvertimeHours
            - capacity.PlannedDowntimeHours;
    }

    private static IReadOnlyList<SmtCapacityWeekColumn> BuildWeekColumns(IReadOnlyList<SmtCapacityReportRow> rows) =>
        rows
            .GroupBy(row => row.WeekName)
            .Select(group =>
            {
                var first = group.First();
                return new SmtCapacityWeekColumn(
                    first.WeekName,
                    first.MonthName,
                    first.MonthSequence,
                    first.MonthSequence == 3 ? 5 : 4);
            })
            .OrderBy(item => item.MonthSequence)
            .ThenBy(item => item.WeekName)
            .ToList();

    private static IReadOnlyList<SmtCapacityLineBlock> BuildLineBlocks(
        IReadOnlyList<SmtCapacityReportRow> rows,
        IReadOnlyList<SmtCapacityLineSummary> summaries) =>
        rows
            .GroupBy(row => new { row.LineCode, row.LineNumber })
            .OrderBy(group => group.Key.LineNumber)
            .ThenBy(group => group.Key.LineCode)
            .Select(group =>
            {
                var familyRows = group
                    .GroupBy(row => row.ProductFamily)
                    .OrderBy(familyGroup => familyGroup.Key)
                    .Select(familyGroup =>
                    {
                        var first = familyGroup.First();
                        var weekCells = familyGroup.ToDictionary(
                            row => row.WeekName,
                            row => new SmtCapacityWeekCell(
                                row.WeeklyDemandUnits,
                                row.MonthlyMps,
                                row.AssignedDemandUnits,
                                row.MaxCapacityUnits,
                                row.UtilizationPercent,
                                row.DeltaCapacityUnits));

                        return new SmtCapacityFamilyLineRow(
                            first.OperationCode,
                            first.LineCode,
                            first.LineNumber,
                            first.ProductFamily,
                            first.SubFamily,
                            first.BusinessUnit,
                            first.LineDistribution,
                            first.UnitsPerHour,
                            first.ChangeoverCount,
                            first.ChangeoverHoursMonth,
                            first.AvailableHours,
                            weekCells);
                    })
                    .ToList();

                var weeklySummaries = summaries
                    .Where(summary => summary.LineCode == group.Key.LineCode)
                    .ToDictionary(summary => summary.WeekName);

                return new SmtCapacityLineBlock(
                    group.Key.LineCode,
                    group.Key.LineNumber,
                    familyRows,
                    weeklySummaries);
            })
            .ToList();
}
