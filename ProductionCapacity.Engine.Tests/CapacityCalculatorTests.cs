using ProductionCapacity.Engine;

namespace ProductionCapacity.Engine.Tests;

public sealed class CapacityCalculatorTests
{
    [Fact]
    public void CalculatesEffectiveCapacityFromAvailableTimeUtilizationAndYield()
    {
        var scenario = CapacityScenario.Create(
            "Base",
            new ProductionWeek("WK13", 5.66m, 7.5m, 3, 0, 0, 0),
            new Machine("ASSY 1", 0.80m),
            new PartNumber("68-6792", "FC3", 300m, 1.00m),
            demandUnits: 1_000m);

        var result = CapacityCalculator.Calculate(scenario);

        Assert.Equal(127.35m, result.AvailableHours);
        Assert.Equal(101.88m, result.EffectiveHours);
        Assert.Equal(1_222.560m, result.CapacityUnits);
        Assert.Equal(81.797m, result.LoadPercent);
        Assert.Equal(222.560m, result.RemainingUnits);
    }

    [Fact]
    public void HolidayDaysReduceTheWeeklyAvailableTime()
    {
        var scenario = CapacityScenario.Create(
            "Holiday",
            new ProductionWeek("WK14", 5.66m, 7.5m, 3, 1, 0, 0),
            new Machine("ASSY 1", 0.80m),
            new PartNumber("68-6792", "FC3", 300m, 1.00m),
            demandUnits: 1_000m);

        var result = CapacityCalculator.Calculate(scenario);

        Assert.Equal(104.85m, result.AvailableHours);
        Assert.Equal(1_006.560m, result.CapacityUnits);
        Assert.Equal(99.350m, result.LoadPercent);
    }

    [Fact]
    public void PartYieldOverrideTakesPriorityOverDefaultYield()
    {
        var scenario = CapacityScenario.Create(
            "New product ramp",
            new ProductionWeek("WK15", 5.66m, 7.5m, 3, 0, 0, 0),
            new Machine("ASSY 1", 0.80m),
            new PartNumber("68-9000", "Nuevo producto", 300m, 1.00m, YieldOverride: 0.70m),
            demandUnits: 1_000m);

        var result = CapacityCalculator.Calculate(scenario);

        Assert.Equal(0.70m, result.AppliedYield);
        Assert.Equal(855.792m, result.CapacityUnits);
        Assert.Equal(116.852m, result.LoadPercent);
    }

    [Fact]
    public void MachinePartUtilizationOverrideTakesPriorityOverMachineFactor()
    {
        var scenario = CapacityScenario.Create(
            "Machine and part override",
            new ProductionWeek("WK16", 5.66m, 7.5m, 3, 0, 0, 0),
            new Machine("ASSY 1", 0.80m),
            new PartNumber("68-6792", "FC3", 300m, 1.00m),
            demandUnits: 1_000m,
            machinePartUtilizationOverride: 0.60m);

        var result = CapacityCalculator.Calculate(scenario);

        Assert.Equal(0.60m, result.AppliedUtilizationFactor);
        Assert.Equal(916.920m, result.CapacityUnits);
        Assert.Equal(109.062m, result.LoadPercent);
    }

    [Fact]
    public void RackConstraintLimitsCapacityAndReportsMissingRacks()
    {
        var scenario = CapacityScenario.Create(
            "Rack constrained",
            new ProductionWeek("WK17", 1m, 1m, 1, 0, 0, 0),
            new Machine("CHAMBER 1", 1.00m),
            new PartNumber("68-RACK", "Rack dependent part", 257.142857m, 1.00m),
            demandUnits: 14m,
            rackConstraint: new RackConstraint("Carro base", 7m, 1));

        var result = CapacityCalculator.Calculate(scenario);

        Assert.Equal(14.001m, result.TimeCapacityUnits);
        Assert.Equal(7m, result.RackCapacityUnits);
        Assert.Equal(7m, result.CapacityUnits);
        Assert.Equal(2, result.RequiredRackCount);
        Assert.Equal(1, result.AvailableRackCount);
        Assert.Equal(1, result.MissingRackCount);
        Assert.True(result.IsRackConstrained);
        Assert.Equal(200.000m, result.LoadPercent);
        Assert.Equal(-7m, result.RemainingUnits);
    }
}
