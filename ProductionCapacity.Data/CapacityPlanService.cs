using Microsoft.EntityFrameworkCore;
using ProductionCapacity.Data.Entities;
using ProductionCapacity.Engine;

namespace ProductionCapacity.Data;

public sealed class CapacityPlanService(PlanningDbContext db)
{
    public async Task<CapacityPlan> GetPlanAsync(string scenarioName, CancellationToken cancellationToken = default)
    {
        var scenario = await db.Scenarios
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.ScenarioName == scenarioName, cancellationToken)
            ?? throw new InvalidOperationException($"Scenario '{scenarioName}' was not found.");

        var week = await db.ProductionWeeks
            .AsNoTracking()
            .Where(item => item.ScenarioId == scenario.ScenarioId)
            .OrderBy(item => item.ProductionWeekId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException($"Scenario '{scenarioName}' has no production weeks.");

        var demands = await db.WeeklyDemand
            .AsNoTracking()
            .Where(item => item.ScenarioId == scenario.ScenarioId && item.ProductionWeekId == week.ProductionWeekId)
            .ToListAsync(cancellationToken);

        var partIds = demands.Select(item => item.PartNumberId).Distinct().ToArray();
        var routes = await db.ProcessRoutes
            .AsNoTracking()
            .Where(item => partIds.Contains(item.PartNumberId) && item.IsCapacityConstrained)
            .OrderBy(item => item.SequenceNumber)
            .ToListAsync(cancellationToken);

        var machineIds = routes.Select(item => item.MachineId).Distinct().ToArray();
        var parts = await db.PartNumbers
            .AsNoTracking()
            .Where(item => partIds.Contains(item.PartNumberId))
            .ToDictionaryAsync(item => item.PartNumberId, cancellationToken);
        var machines = await db.Machines
            .AsNoTracking()
            .Where(item => machineIds.Contains(item.MachineId))
            .ToDictionaryAsync(item => item.MachineId, cancellationToken);
        var partOverrides = await db.ScenarioPartOverrides
            .AsNoTracking()
            .Where(item => item.ScenarioId == scenario.ScenarioId)
            .ToDictionaryAsync(item => item.PartNumberId, cancellationToken);
        var machineOverrides = await db.ScenarioMachineOverrides
            .AsNoTracking()
            .Where(item => item.ScenarioId == scenario.ScenarioId)
            .ToDictionaryAsync(item => item.MachineId, cancellationToken);
        var machinePartOverrides = await db.ScenarioMachinePartOverrides
            .AsNoTracking()
            .Where(item => item.ScenarioId == scenario.ScenarioId)
            .ToListAsync(cancellationToken);
        var rackConfigurations = await db.RackConfigurations
            .AsNoTracking()
            .Where(item =>
                item.IsActive &&
                partIds.Contains(item.PartNumberId) &&
                machineIds.Contains(item.MachineId))
            .ToListAsync(cancellationToken);
        var rackTypeIds = rackConfigurations.Select(item => item.RackTypeId).Distinct().ToArray();
        var rackTypes = await db.RackTypes
            .AsNoTracking()
            .Where(item => rackTypeIds.Contains(item.RackTypeId))
            .ToDictionaryAsync(item => item.RackTypeId, cancellationToken);
        var rackInventories = await db.ScenarioRackInventories
            .AsNoTracking()
            .Where(item =>
                item.ScenarioId == scenario.ScenarioId &&
                item.ProductionWeekId == week.ProductionWeekId &&
                machineIds.Contains(item.MachineId) &&
                rackTypeIds.Contains(item.RackTypeId))
            .ToListAsync(cancellationToken);

        var machinePartOverrideByKey = machinePartOverrides.ToDictionary(
            item => (item.MachineId, item.PartNumberId));
        var rackConfigurationByKey = rackConfigurations
            .GroupBy(item => (item.MachineId, item.PartNumberId))
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(item => item.RackAccessoryId.HasValue)
                    .ThenBy(item => item.RackConfigurationId)
                    .First());
        var rackInventoryByKey = rackInventories.ToDictionary(
            item => (item.MachineId, item.RackTypeId));
        var demandByPartId = demands.ToDictionary(item => item.PartNumberId);
        var planWeek = new CapacityWeekInput
        {
            Name = week.WeekName,
            WorkDays = week.WorkDays,
            HoursPerShift = week.HoursPerShift,
            ShiftsPerDay = week.ShiftsPerDay,
            HolidayDays = week.HolidayDays,
            OvertimeHours = week.OvertimeHours,
            PlannedDowntimeHours = week.PlannedDowntimeHours
        };

        var rows = new List<CapacityRowInput>();
        foreach (var route in routes)
        {
            if (!parts.TryGetValue(route.PartNumberId, out var part) ||
                !machines.TryGetValue(route.MachineId, out var machine) ||
                !demandByPartId.TryGetValue(route.PartNumberId, out var demand))
            {
                continue;
            }

            machinePartOverrideByKey.TryGetValue((route.MachineId, route.PartNumberId), out var machinePartOverride);
            partOverrides.TryGetValue(route.PartNumberId, out var partOverride);
            machineOverrides.TryGetValue(route.MachineId, out var machineOverride);

            var utilizationFactor =
                machinePartOverride?.UtilizationFactorOverride ??
                machineOverride?.UtilizationFactorOverride ??
                route.DefaultUtilizationFactor ??
                machine.DefaultUtilizationFactor;
            var yield =
                machinePartOverride?.YieldOverride ??
                partOverride?.YieldOverride ??
                route.DefaultYield ??
                part.DefaultYield;
            var standardTimeSeconds =
                machinePartOverride?.StandardTimeSecondsOverride ??
                route.StandardTimeSeconds;
            rackConfigurationByKey.TryGetValue((route.MachineId, route.PartNumberId), out var rackConfiguration);
            RackType? rackType = null;
            ScenarioRackInventory? rackInventory = null;
            if (rackConfiguration is not null)
            {
                rackTypes.TryGetValue(rackConfiguration.RackTypeId, out rackType);
                rackInventoryByKey.TryGetValue((route.MachineId, rackConfiguration.RackTypeId), out rackInventory);
            }

            rows.Add(new CapacityRowInput
            {
                MachineName = machine.MachineName,
                UtilizationPercent = utilizationFactor * 100m,
                PartNumber = part.PartNumber,
                PartDescription = part.Description,
                StandardTimeSeconds = standardTimeSeconds,
                YieldPercent = yield * 100m,
                DemandUnits = demand.DemandUnits,
                RackName = rackType?.RackName,
                UnitsPerRack = rackConfiguration?.UnitsPerRack,
                AvailableRackCount = rackConfiguration is null ? null : rackInventory?.AvailableRackCount ?? 0
            });
        }

        var results = rows
            .Select(row => CapacityCalculator.Calculate(CapacityScenario.Create(
                scenario.ScenarioName,
                planWeek.ToEngineWeek(),
                new Machine(row.MachineName, row.UtilizationPercent / 100m),
                new PartNumber(
                    row.PartNumber,
                    row.PartDescription,
                    row.StandardTimeSeconds,
                    row.YieldPercent / 100m),
                row.DemandUnits,
                rackConstraint: row.ToRackConstraint())))
            .ToList();

        return new CapacityPlan(scenario.ScenarioName, planWeek, rows, results);
    }
}
