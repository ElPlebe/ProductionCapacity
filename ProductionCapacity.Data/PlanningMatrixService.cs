using Microsoft.EntityFrameworkCore;
using ProductionCapacity.Engine;

namespace ProductionCapacity.Data;

public sealed class PlanningMatrixService(PlanningDbContext db)
{
    public async Task<IReadOnlyList<PlanningMatrix>> GetMatricesAsync(
        string scenarioName,
        CancellationToken cancellationToken = default)
    {
        var scenario = await db.Scenarios
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.ScenarioName == scenarioName, cancellationToken)
            ?? throw new InvalidOperationException($"Scenario '{scenarioName}' was not found.");

        var weekIds = await db.ProductionWeeks
            .AsNoTracking()
            .Where(item => item.ScenarioId == scenario.ScenarioId)
            .OrderBy(item => item.ProductionWeekId)
            .Select(item => item.ProductionWeekId)
            .ToListAsync(cancellationToken);

        var matrices = new List<PlanningMatrix>();
        foreach (var weekId in weekIds)
        {
            matrices.Add(await GetMatrixAsync(scenarioName, weekId, cancellationToken));
        }

        return matrices;
    }

    public async Task<PlanningMatrix> GetMatrixAsync(
        string scenarioName,
        int? productionWeekId = null,
        CancellationToken cancellationToken = default)
    {
        var scenario = await db.Scenarios
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.ScenarioName == scenarioName, cancellationToken)
            ?? throw new InvalidOperationException($"Scenario '{scenarioName}' was not found.");

        var weekQuery = db.ProductionWeeks
            .AsNoTracking()
            .Where(item => item.ScenarioId == scenario.ScenarioId);

        if (productionWeekId is not null)
        {
            weekQuery = weekQuery.Where(item => item.ProductionWeekId == productionWeekId.Value);
        }

        var week = await weekQuery
            .OrderBy(item => item.ProductionWeekId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException($"Scenario '{scenarioName}' has no production weeks.");

        var demands = await db.WeeklyDemand
            .AsNoTracking()
            .Where(item => item.ScenarioId == scenario.ScenarioId && item.ProductionWeekId == week.ProductionWeekId)
            .ToListAsync(cancellationToken);
        var partIds = demands.Select(item => item.PartNumberId).Distinct().ToArray();

        var parts = await db.PartNumbers
            .AsNoTracking()
            .Where(item => partIds.Contains(item.PartNumberId))
            .ToDictionaryAsync(item => item.PartNumberId, cancellationToken);

        var rates = await db.OperationRates
            .AsNoTracking()
            .Where(item => item.IsActive && partIds.Contains(item.PartNumberId))
            .ToListAsync(cancellationToken);
        var operationIds = rates.Select(item => item.OperationId).Distinct().ToArray();

        var operations = await db.Operations
            .AsNoTracking()
            .Where(item => item.IsActive && operationIds.Contains(item.OperationId))
            .OrderBy(item => item.SequenceNumber)
            .ThenBy(item => item.OperationCode)
            .ToListAsync(cancellationToken);

        var capacities = await db.OperationWeeklyCapacities
            .AsNoTracking()
            .Where(item =>
                item.ScenarioId == scenario.ScenarioId &&
                item.ProductionWeekId == week.ProductionWeekId &&
                operationIds.Contains(item.OperationId))
            .ToDictionaryAsync(item => item.OperationId, cancellationToken);

        var operationById = operations.ToDictionary(item => item.OperationId);
        var capacityResults = new List<OperationCapacityResult>();
        var rowCells = new Dictionary<int, Dictionary<string, PlanningMatrixCell>>();

        foreach (var demand in demands)
        {
            var partRates = rates.Where(item => item.PartNumberId == demand.PartNumberId);
            foreach (var rate in partRates)
            {
                if (!operationById.TryGetValue(rate.OperationId, out var operation) ||
                    !capacities.TryGetValue(rate.OperationId, out var capacity))
                {
                    continue;
                }

                var result = OperationCapacityCalculator.Calculate(new OperationCapacityScenario(
                    scenario.ScenarioName,
                    parts[demand.PartNumberId].PartNumber,
                    operation.OperationCode,
                    operation.OperationName,
                    demand.DemandUnits,
                    new OperationWeekCapacity(
                        week.WeekName,
                        capacity.WorkDays,
                        capacity.HoursPerShift,
                        capacity.ShiftsPerDay,
                        capacity.EquipmentQuantity,
                        capacity.HolidayDays,
                        capacity.OvertimeHours,
                        capacity.PlannedDowntimeHours),
                    new OperationRate(
                        rate.StandardTimeSeconds,
                        rate.YieldFactor,
                        rate.UtilizationFactor,
                        rate.EfficiencyFactor)));

                capacityResults.Add(result);

                if (!rowCells.TryGetValue(demand.PartNumberId, out var cells))
                {
                    cells = new Dictionary<string, PlanningMatrixCell>();
                    rowCells[demand.PartNumberId] = cells;
                }

                cells[operation.OperationCode] = new PlanningMatrixCell(
                    operation.OperationCode,
                    operation.OperationName,
                    result.LoadHours,
                    result.EffectiveUnitsPerHour,
                    result.TimeCapacityUnits,
                    result.TimeCapacityUnits);
            }
        }

        var partRows = demands
            .OrderBy(item => parts[item.PartNumberId].PartNumber)
            .Select(item => new PlanningMatrixRow(
                item.PartNumberId,
                parts[item.PartNumberId].PartNumber,
                GetFamily(parts[item.PartNumberId]),
                parts[item.PartNumberId].Description,
                item.DemandUnits,
                rowCells.TryGetValue(item.PartNumberId, out var cells)
                    ? cells
                    : new Dictionary<string, PlanningMatrixCell>(),
                []))
            .ToList();

        var rows = partRows
            .GroupBy(item => item.ProductFamily)
            .OrderBy(group => group.Key)
            .Select(group =>
            {
                var children = group
                    .OrderBy(item => item.PartNumber)
                    .ToList();
                var cells = operations
                    .Select(operation => CreateFamilyCell(operation.OperationCode, operation.OperationName, children))
                    .Where(cell => cell is not null)
                    .ToDictionary(cell => cell!.OperationCode, cell => cell!);

                return new PlanningMatrixRow(
                    0,
                    group.Key,
                    group.Key,
                    $"{children.Count} numero(s) de parte",
                    children.Sum(item => item.DemandUnits),
                    cells,
                    children);
            })
            .ToList();

        var summaries = operations.ToDictionary(
            operation => operation.OperationCode,
            operation =>
            {
                capacities.TryGetValue(operation.OperationId, out var capacity);
                var availableHours = capacity is null
                    ? 0m
                    : new OperationWeekCapacity(
                        week.WeekName,
                        capacity.WorkDays,
                        capacity.HoursPerShift,
                        capacity.ShiftsPerDay,
                        capacity.EquipmentQuantity,
                        capacity.HolidayDays,
                        capacity.OvertimeHours,
                        capacity.PlannedDowntimeHours).AvailableHours;
                var totalLoad = capacityResults
                    .Where(item => item.OperationCode == operation.OperationCode)
                    .Sum(item => item.LoadHours);
                var loadPercent = availableHours == 0m
                    ? 0m
                    : DecimalRounding.RoundUp((totalLoad / availableHours) * 100m);

                return new PlanningOperationSummary(
                    operation.OperationCode,
                    operation.OperationName,
                    availableHours,
                    DecimalRounding.RoundUp(totalLoad),
                    DecimalRounding.RoundUp(availableHours - totalLoad),
                    loadPercent);
            });

        return new PlanningMatrix(
            scenario.ScenarioName,
            week.WeekName,
            operations
                .Select(item => new PlanningOperationColumn(
                    item.OperationId,
                    item.OperationCode,
                    item.OperationName,
                    item.SequenceNumber))
                .ToList(),
            rows,
            summaries);
    }

    private static PlanningMatrixCell? CreateFamilyCell(
        string operationCode,
        string operationName,
        IReadOnlyList<PlanningMatrixRow> children)
    {
        var childCells = children
            .Select(row => row.Cells.TryGetValue(operationCode, out var cell) ? cell : null)
            .Where(cell => cell is not null)
            .Cast<PlanningMatrixCell>()
            .ToList();
        if (childCells.Count == 0)
        {
            return null;
        }

        var loadHours = childCells.Sum(item => item.LoadHours);
        var demandUnits = children.Sum(item => item.Cells.ContainsKey(operationCode) ? item.DemandUnits : 0m);
        var effectiveUnitsPerHour = loadHours == 0m
            ? 0m
            : DecimalRounding.RoundUp(demandUnits / loadHours, 6);

        return new PlanningMatrixCell(
            operationCode,
            operationName,
            DecimalRounding.RoundUp(loadHours),
            effectiveUnitsPerHour,
            DecimalRounding.RoundUp(childCells.Sum(item => item.TimeCapacityUnits)),
            DecimalRounding.RoundUp(childCells.Sum(item => item.CapacityUnits)));
    }

    private static string GetFamily(Entities.PartNumberEntity part) =>
        string.IsNullOrWhiteSpace(part.ProductFamily)
            ? part.PartNumber
            : part.ProductFamily.Trim();
}
