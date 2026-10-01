namespace ProductionCapacity.Data;

public sealed record FiscalQuarterPeriod(
    int FiscalQuarter,
    int FiscalYear,
    int MonthSequence,
    int CalendarMonth,
    int CalendarYear,
    string MonthLabel,
    int WeekCount);

public static class FiscalQuarterCalendar
{
    private static readonly string[] SpanishMonthNames =
    [
        "Enero",
        "Febrero",
        "Marzo",
        "Abril",
        "Mayo",
        "Junio",
        "Julio",
        "Agosto",
        "Septiembre",
        "Octubre",
        "Noviembre",
        "Diciembre"
    ];

    public static FiscalQuarterPeriod GetPeriod(int fiscalQuarter, int fiscalYear, int monthSequence)
    {
        if (fiscalQuarter is < 1 or > 4)
        {
            throw new ArgumentOutOfRangeException(nameof(fiscalQuarter), "Fiscal quarter must be between 1 and 4.");
        }

        if (monthSequence is < 1 or > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(monthSequence), "Month sequence must be M1, M2 or M3.");
        }

        var month = fiscalQuarter switch
        {
            1 => monthSequence switch { 1 => 8, 2 => 9, _ => 10 },
            2 => monthSequence switch { 1 => 11, 2 => 12, _ => 1 },
            3 => monthSequence switch { 1 => 2, 2 => 3, _ => 4 },
            _ => monthSequence switch { 1 => 5, 2 => 6, _ => 7 }
        };
        var calendarYear = fiscalQuarter == 2 && monthSequence == 3
            ? fiscalYear + 1
            : fiscalYear;
        var weekCount = monthSequence == 3 ? 5 : 4;
        var monthLabel = $"{SpanishMonthNames[month - 1]} {calendarYear}";

        return new FiscalQuarterPeriod(
            fiscalQuarter,
            fiscalYear,
            monthSequence,
            month,
            calendarYear,
            monthLabel,
            weekCount);
    }
}
