using ProductionCapacity.Engine;

namespace ProductionCapacity.Data;

public sealed record CapacityPlan(
    string ScenarioName,
    CapacityWeekInput Week,
    IReadOnlyList<CapacityRowInput> Rows,
    IReadOnlyList<CapacityResult> Results);

public sealed class CapacityWeekInput
{
    public string Name { get; set; } = string.Empty;
    public decimal WorkDays { get; set; }
    public decimal HoursPerShift { get; set; }
    public int ShiftsPerDay { get; set; }
    public decimal HolidayDays { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal PlannedDowntimeHours { get; set; }

    public ProductionWeek ToEngineWeek() =>
        new(Name, WorkDays, HoursPerShift, ShiftsPerDay, HolidayDays, OvertimeHours, PlannedDowntimeHours);
}

public sealed class CapacityRowInput
{
    public string MachineName { get; init; } = string.Empty;
    public decimal UtilizationPercent { get; set; }
    public string PartNumber { get; init; } = string.Empty;
    public string PartDescription { get; init; } = string.Empty;
    public decimal StandardTimeSeconds { get; set; }
    public decimal YieldPercent { get; set; }
    public decimal DemandUnits { get; set; }
    public string? RackName { get; init; }
    public decimal? UnitsPerRack { get; set; }
    public int? AvailableRackCount { get; set; }

    public RackConstraint? ToRackConstraint() =>
        string.IsNullOrWhiteSpace(RackName) || UnitsPerRack is not > 0 || AvailableRackCount is null
            ? null
            : new RackConstraint(RackName, UnitsPerRack.Value, AvailableRackCount.Value);
}
