using ProductionCapacity.Engine;

namespace ProductionCapacity.Engine.Tests;

public sealed class TestTimeCalculatorTests
{
    [Fact]
    public void CalculatesStandardTimeSecondsFromTestHandlingCellAndEquipment()
    {
        var definition = new TestTimeDefinition(
            TestTimeMinutes: 120m,
            HandlingMinutes: 5m,
            QuantityCell: 2m,
            QuantityEquipment: 1m,
            ManualStandardTimeSeconds: null);

        var seconds = TestTimeCalculator.CalculateStandardTimeSeconds(definition);

        Assert.Equal(3_750m, seconds);
    }

    [Fact]
    public void ManualStandardTimeOverrideTakesPriority()
    {
        var definition = new TestTimeDefinition(
            TestTimeMinutes: 120m,
            HandlingMinutes: 5m,
            QuantityCell: 2m,
            QuantityEquipment: 1m,
            ManualStandardTimeSeconds: 3_600m);

        var seconds = TestTimeCalculator.CalculateStandardTimeSeconds(definition);

        Assert.Equal(3_600m, seconds);
    }
}
