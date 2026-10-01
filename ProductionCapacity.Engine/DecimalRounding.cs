using System.Globalization;

namespace ProductionCapacity.Engine;

public static class DecimalRounding
{
    private const int DefaultDecimals = 3;

    public static decimal RoundUp(decimal value, int decimals = DefaultDecimals)
    {
        if (decimals < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(decimals), "Decimals must be zero or greater.");
        }

        var factor = 1m;
        for (var index = 0; index < decimals; index++)
        {
            factor *= 10m;
        }

        return Math.Ceiling(value * factor) / factor;
    }

    public static string FormatUp(decimal value, string? suffix = null, int decimals = DefaultDecimals)
    {
        var formatted = RoundUp(value, decimals).ToString($"N{decimals}", CultureInfo.CurrentCulture);
        return string.IsNullOrWhiteSpace(suffix) ? formatted : $"{formatted}{suffix}";
    }

    public static decimal RoundNatural(decimal value, int decimals = DefaultDecimals)
    {
        if (decimals < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(decimals), "Decimals must be zero or greater.");
        }

        return Math.Round(value, decimals, MidpointRounding.AwayFromZero);
    }

    public static string FormatNatural(decimal value, string? suffix = null, int decimals = DefaultDecimals)
    {
        var formatted = RoundNatural(value, decimals).ToString($"N{decimals}", CultureInfo.CurrentCulture);
        return string.IsNullOrWhiteSpace(suffix) ? formatted : $"{formatted}{suffix}";
    }

    public static string FormatPercentNatural(decimal value, string? suffix = "%", int decimals = DefaultDecimals) =>
        FormatNatural(value * 100m, suffix, decimals);

    public static string FormatOptionalUp(decimal? value, string? suffix = null, int decimals = DefaultDecimals) =>
        value.HasValue ? FormatUp(value.Value, suffix, decimals) : "-";
}
