namespace ProductionCapacity.Engine;

public sealed record TestTimeDefinition(
    decimal TestTimeMinutes,
    decimal HandlingMinutes,
    decimal QuantityCell,
    decimal QuantityEquipment,
    decimal? ManualStandardTimeSeconds);

public static class TestTimeCalculator
{
    public static decimal CalculateStandardTimeSeconds(TestTimeDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (definition.ManualStandardTimeSeconds is > 0)
        {
            return RoundSeconds(definition.ManualStandardTimeSeconds.Value);
        }

        if (definition.QuantityCell <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(definition), "Cell quantity must be greater than zero.");
        }

        if (definition.QuantityEquipment <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(definition), "Equipment quantity must be greater than zero.");
        }

        if (definition.TestTimeMinutes < 0 || definition.HandlingMinutes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(definition), "Test and handling time cannot be negative.");
        }

        return RoundSeconds(((definition.TestTimeMinutes + definition.HandlingMinutes) /
            definition.QuantityCell /
            definition.QuantityEquipment) * 60m);
    }

    private static decimal RoundSeconds(decimal value) =>
        Math.Round(value, 6, MidpointRounding.AwayFromZero);
}
