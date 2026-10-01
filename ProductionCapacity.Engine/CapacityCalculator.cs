namespace ProductionCapacity.Engine;

public sealed record ProductionWeek(
    string Name,
    decimal WorkDays,
    decimal HoursPerShift,
    int ShiftsPerDay,
    decimal HolidayDays,
    decimal OvertimeHours,
    decimal PlannedDowntimeHours)
{
    public decimal AvailableHours =>
        Math.Max(0, ((WorkDays - HolidayDays) * HoursPerShift * ShiftsPerDay) + OvertimeHours - PlannedDowntimeHours);
}

public sealed record Machine(string Name, decimal UtilizationFactor);

public sealed record RackConstraint(
    string RackName,
    decimal UnitsPerRack,
    int AvailableRackCount)
{
    public decimal RackCapacityUnits => UnitsPerRack * AvailableRackCount;

    public int GetRequiredRackCount(decimal demandUnits) =>
        demandUnits <= 0 ? 0 : (int)Math.Ceiling(demandUnits / UnitsPerRack);
}

public sealed record PartNumber(
    string Number,
    string Description,
    decimal StandardTimeSeconds,
    decimal DefaultYield,
    decimal? YieldOverride = null)
{
    public decimal AppliedYield => YieldOverride ?? DefaultYield;
}

public sealed record CapacityScenario(
    string Name,
    ProductionWeek Week,
    Machine Machine,
    PartNumber Part,
    decimal DemandUnits,
    decimal? MachinePartUtilizationOverride,
    RackConstraint? RackConstraint)
{
    public static CapacityScenario Create(
        string name,
        ProductionWeek week,
        Machine machine,
        PartNumber part,
        decimal demandUnits,
        decimal? machinePartUtilizationOverride = null,
        RackConstraint? rackConstraint = null)
    {
        return new CapacityScenario(name, week, machine, part, demandUnits, machinePartUtilizationOverride, rackConstraint);
    }

    public decimal AppliedUtilizationFactor => MachinePartUtilizationOverride ?? Machine.UtilizationFactor;
}

public sealed record CapacityResult(
    string ScenarioName,
    string WeekName,
    string MachineName,
    string PartNumber,
    string PartDescription,
    decimal DemandUnits,
    decimal AvailableHours,
    decimal EffectiveHours,
    decimal AppliedYield,
    decimal AppliedUtilizationFactor,
    decimal CapacityUnits,
    decimal RequiredHours,
    decimal LoadPercent,
    decimal RemainingUnits,
    decimal TimeCapacityUnits = 0,
    string? RackName = null,
    decimal? UnitsPerRack = null,
    int? RequiredRackCount = null,
    int? AvailableRackCount = null,
    int? MissingRackCount = null,
    decimal? RackCapacityUnits = null,
    bool IsRackConstrained = false);

public static class CapacityCalculator
{
    public static CapacityResult Calculate(CapacityScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        if (scenario.Part.StandardTimeSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(scenario), "Standard time must be greater than zero.");
        }

        if (scenario.RackConstraint is { UnitsPerRack: <= 0 })
        {
            throw new ArgumentOutOfRangeException(nameof(scenario), "Units per rack must be greater than zero.");
        }

        if (scenario.RackConstraint is { AvailableRackCount: < 0 })
        {
            throw new ArgumentOutOfRangeException(nameof(scenario), "Available rack count cannot be negative.");
        }

        var availableHours = RoundHours(scenario.Week.AvailableHours);
        var effectiveHours = RoundHours(availableHours * scenario.AppliedUtilizationFactor * scenario.Part.AppliedYield);
        var timeCapacityUnits = DecimalRounding.RoundUp((effectiveHours * 3600m) / scenario.Part.StandardTimeSeconds, 3);
        var capacityUnits = timeCapacityUnits;
        decimal? rackCapacityUnits = null;
        int? requiredRackCount = null;
        int? availableRackCount = null;
        int? missingRackCount = null;
        var isRackConstrained = false;

        if (scenario.RackConstraint is { } rackConstraint)
        {
            rackCapacityUnits = rackConstraint.RackCapacityUnits;
            requiredRackCount = rackConstraint.GetRequiredRackCount(scenario.DemandUnits);
            availableRackCount = rackConstraint.AvailableRackCount;
            missingRackCount = Math.Max(0, requiredRackCount.Value - availableRackCount.Value);
            capacityUnits = Math.Min(timeCapacityUnits, rackCapacityUnits.Value);
            isRackConstrained = rackCapacityUnits.Value < timeCapacityUnits || missingRackCount.Value > 0;
        }

        var requiredHours = RoundHours((scenario.DemandUnits * scenario.Part.StandardTimeSeconds) / 3600m);
        var loadPercent = isRackConstrained
            ? CalculateUnitLoadPercent(scenario.DemandUnits, capacityUnits)
            : effectiveHours == 0
            ? 0
            : DecimalRounding.RoundUp((requiredHours / effectiveHours) * 100m);
        var remainingUnits = capacityUnits - scenario.DemandUnits;

        return new CapacityResult(
            scenario.Name,
            scenario.Week.Name,
            scenario.Machine.Name,
            scenario.Part.Number,
            scenario.Part.Description,
            scenario.DemandUnits,
            availableHours,
            effectiveHours,
            scenario.Part.AppliedYield,
            scenario.AppliedUtilizationFactor,
            capacityUnits,
            requiredHours,
            loadPercent,
            remainingUnits,
            timeCapacityUnits,
            scenario.RackConstraint?.RackName,
            scenario.RackConstraint?.UnitsPerRack,
            requiredRackCount,
            availableRackCount,
            missingRackCount,
            rackCapacityUnits,
            isRackConstrained);
    }

    private static decimal RoundHours(decimal value) =>
        DecimalRounding.RoundUp(value);

    private static decimal CalculateUnitLoadPercent(decimal demandUnits, decimal capacityUnits) =>
        capacityUnits == 0
            ? 0
            : DecimalRounding.RoundUp((demandUnits / capacityUnits) * 100m);
}
