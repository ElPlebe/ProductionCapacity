using ProductionCapacity.Engine;

namespace ProductionCapacity.Engine.Tests;

public sealed class OperationCapacityCalculatorTests
{
    [Fact]
    public void CalculatesAvailableHoursUsingOperationEquipmentQuantity()
    {
        var capacity = new OperationWeekCapacity(
            "WK13",
            WorkDays: 5.66m,
            HoursPerShift: 7.5m,
            ShiftsPerDay: 3,
            EquipmentQuantity: 2m,
            HolidayDays: 0m,
            OvertimeHours: 0m,
            PlannedDowntimeHours: 0m);

        Assert.Equal(254.700m, capacity.AvailableHours);
    }

    [Fact]
    public void CalculatesEffectiveRateAndLoadHoursFromDemand()
    {
        var scenario = new OperationCapacityScenario(
            "Base semanal",
            "68-103450",
            "ASSY",
            "ASSY",
            DemandUnits: 1_000m,
            new OperationWeekCapacity("WK13", 5.66m, 7.5m, 3, 2m, 0m, 0m, 0m),
            new OperationRate(StandardTimeSeconds: 300m, Yield: 0.95m, UtilizationFactor: 0.80m, EfficiencyFactor: 0.90m));

        var result = OperationCapacityCalculator.Calculate(scenario);

        Assert.Equal(8.208m, result.EffectiveUnitsPerHour);
        Assert.Equal(121.833m, result.LoadHours);
        Assert.Equal(2_090.578m, result.TimeCapacityUnits);
        Assert.Equal(132.867m, result.RemainingHours);
        Assert.Equal(47.834m, result.LoadPercent);
    }

    [Fact]
    public void MatchesSampledExcelPlanUabuAssyLoadForKnownPart()
    {
        var scenario = new OperationCapacityScenario(
            "Base semanal",
            "68-103450",
            "ASSY",
            "ASSY",
            DemandUnits: 1_000m,
            new OperationWeekCapacity("WK13", 5.66m, 7.5m, 3, 2m, 0m, 0m, 0m),
            new OperationRate(StandardTimeSeconds: 73.8684m, Yield: 1m, UtilizationFactor: 1m, EfficiencyFactor: 1m));

        var result = OperationCapacityCalculator.Calculate(scenario);

        Assert.Equal(20.519m, result.LoadHours);
        Assert.Equal(254.700m, result.AvailableHours);
        Assert.Equal(234.181m, result.RemainingHours);
    }
}
