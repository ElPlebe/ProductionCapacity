namespace ProductionCapacity.Engine;

public sealed record OperationWeekCapacity(
    string WeekName,
    decimal WorkDays,
    decimal HoursPerShift,
    int ShiftsPerDay,
    decimal EquipmentQuantity,
    decimal HolidayDays,
    decimal OvertimeHours,
    decimal PlannedDowntimeHours)
{
    public decimal AvailableHours => RoundHours(
        Math.Max(0m, ((WorkDays - HolidayDays) * HoursPerShift * ShiftsPerDay * EquipmentQuantity) +
            OvertimeHours -
            PlannedDowntimeHours));

    private static decimal RoundHours(decimal value) =>
        Math.Round(value, 3, MidpointRounding.AwayFromZero);
}

public sealed record OperationRate(
    decimal StandardTimeSeconds,
    decimal Yield,
    decimal UtilizationFactor,
    decimal EfficiencyFactor)
{
    public decimal BaseUnitsPerHour
    {
        get
        {
            Validate();
            return 3600m / StandardTimeSeconds;
        }
    }

    public decimal EffectiveUnitsPerHour
    {
        get
        {
            Validate();
            return BaseUnitsPerHour * Yield * UtilizationFactor * EfficiencyFactor;
        }
    }

    public void Validate()
    {
        if (StandardTimeSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(StandardTimeSeconds), "Standard time must be greater than zero.");
        }

        if (Yield < 0 || UtilizationFactor < 0 || EfficiencyFactor < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(OperationRate), "Rate factors cannot be negative.");
        }
    }
}

public sealed record OperationCapacityScenario(
    string ScenarioName,
    string PartNumber,
    string OperationCode,
    string OperationName,
    decimal DemandUnits,
    OperationWeekCapacity Capacity,
    OperationRate Rate);

public sealed record OperationCapacityResult(
    string ScenarioName,
    string WeekName,
    string PartNumber,
    string OperationCode,
    string OperationName,
    decimal DemandUnits,
    decimal AvailableHours,
    decimal EffectiveUnitsPerHour,
    decimal LoadHours,
    decimal TimeCapacityUnits,
    decimal RemainingHours,
    decimal LoadPercent);

public static class OperationCapacityCalculator
{
    public static OperationCapacityResult Calculate(OperationCapacityScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(scenario.Capacity);
        ArgumentNullException.ThrowIfNull(scenario.Rate);

        scenario.Rate.Validate();

        var availableHours = scenario.Capacity.AvailableHours;
        var effectiveUnitsPerHour = RoundUnits(scenario.Rate.EffectiveUnitsPerHour);
        var loadHours = effectiveUnitsPerHour == 0m
            ? 0m
            : RoundHours(scenario.DemandUnits / scenario.Rate.EffectiveUnitsPerHour);
        var timeCapacityUnits = RoundUnits(availableHours * scenario.Rate.EffectiveUnitsPerHour);
        var remainingHours = RoundHours(availableHours - loadHours);
        var loadPercent = availableHours == 0m
            ? 0m
            : RoundPercent((loadHours / availableHours) * 100m);

        return new OperationCapacityResult(
            scenario.ScenarioName,
            scenario.Capacity.WeekName,
            scenario.PartNumber,
            scenario.OperationCode,
            scenario.OperationName,
            scenario.DemandUnits,
            availableHours,
            effectiveUnitsPerHour,
            loadHours,
            timeCapacityUnits,
            remainingHours,
            loadPercent);
    }

    private static decimal RoundHours(decimal value) =>
        DecimalRounding.RoundUp(value);

    private static decimal RoundUnits(decimal value) =>
        DecimalRounding.RoundUp(value);

    private static decimal RoundPercent(decimal value) =>
        DecimalRounding.RoundUp(value);
}
