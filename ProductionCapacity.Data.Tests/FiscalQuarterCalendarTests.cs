using ProductionCapacity.Data;

namespace ProductionCapacity.Data.Tests;

public sealed class FiscalQuarterCalendarTests
{
    [Theory]
    [InlineData(1, 2026, 1, "Agosto 2026", 4)]
    [InlineData(1, 2026, 2, "Septiembre 2026", 4)]
    [InlineData(1, 2026, 3, "Octubre 2026", 5)]
    [InlineData(2, 2026, 1, "Noviembre 2026", 4)]
    [InlineData(2, 2026, 2, "Diciembre 2026", 4)]
    [InlineData(2, 2026, 3, "Enero 2027", 5)]
    [InlineData(3, 2026, 1, "Febrero 2026", 4)]
    [InlineData(3, 2026, 2, "Marzo 2026", 4)]
    [InlineData(3, 2026, 3, "Abril 2026", 5)]
    [InlineData(4, 2026, 1, "Mayo 2026", 4)]
    [InlineData(4, 2026, 2, "Junio 2026", 4)]
    [InlineData(4, 2026, 3, "Julio 2026", 5)]
    public void MapsFiscalQuarterMonthsAndWeeks(
        int quarter,
        int fiscalYear,
        int monthSequence,
        string expectedLabel,
        int expectedWeeks)
    {
        var period = FiscalQuarterCalendar.GetPeriod(quarter, fiscalYear, monthSequence);

        Assert.Equal(expectedLabel, period.MonthLabel);
        Assert.Equal(expectedWeeks, period.WeekCount);
    }
}
