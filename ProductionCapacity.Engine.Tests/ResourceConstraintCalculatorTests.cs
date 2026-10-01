using ProductionCapacity.Engine;

namespace ProductionCapacity.Engine.Tests;

public sealed class ResourceConstraintCalculatorTests
{
    [Fact]
    public void CalculatesMissingResourcesForChasisOrChambers()
    {
        var constraint = new PlanningResourceConstraint(
            ResourceName: "Chasis Shockwave",
            ResourceKind: "Chasis",
            UnitsPerResource: 7m,
            AvailableResourceCount: 1);

        var result = ResourceConstraintCalculator.Calculate(14m, 2_000m, constraint);

        Assert.Equal(2, result.RequiredResourceCount);
        Assert.Equal(1, result.AvailableResourceCount);
        Assert.Equal(1, result.MissingResourceCount);
        Assert.Equal(7m, result.ResourceCapacityUnits);
        Assert.Equal(7m, result.ConstrainedCapacityUnits);
        Assert.True(result.IsConstrained);
    }
}
