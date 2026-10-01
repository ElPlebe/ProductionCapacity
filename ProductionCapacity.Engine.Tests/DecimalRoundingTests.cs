namespace ProductionCapacity.Engine.Tests;

public sealed class DecimalRoundingTests
{
    [Fact]
    public void RoundUpAlwaysUsesThreeDecimalsByDefault()
    {
        Assert.Equal(90.001m, DecimalRounding.RoundUp(90.0001m));
        Assert.Equal(90.000m, DecimalRounding.RoundUp(90m));
    }

    [Fact]
    public void FormatUpKeepsTrailingZeros()
    {
        Assert.Equal("90.000", DecimalRounding.FormatUp(90m));
        Assert.Equal("90.001%", DecimalRounding.FormatUp(90.0001m, "%"));
    }

    [Fact]
    public void FormatNaturalUsesStandardRoundingAndRequestedDecimals()
    {
        Assert.Equal("2.91", DecimalRounding.FormatNatural(2.909m, decimals: 2));
        Assert.Equal("2.90", DecimalRounding.FormatNatural(2.904m, decimals: 2));
        Assert.Equal("2%", DecimalRounding.FormatNatural(1.907m, "%", decimals: 0));
    }

    [Fact]
    public void FormatPercentNaturalConvertsFactorToPercentage()
    {
        Assert.Equal("100%", DecimalRounding.FormatPercentNatural(1m, decimals: 0));
        Assert.Equal("50%", DecimalRounding.FormatPercentNatural(0.5m, decimals: 0));
        Assert.Equal("12.5%", DecimalRounding.FormatPercentNatural(0.125m, decimals: 1));
    }
}
