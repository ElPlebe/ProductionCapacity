using System.Globalization;

namespace ProductionCapacity.Data;

public enum SmtCapacitySortColumn
{
    WeekName,
    MonthName,
    LineCode,
    ProductFamily,
    SubFamily,
    BusinessUnit,
    LineDistribution,
    WeeklyDemandUnits,
    MonthlyMps,
    AssignedDemandUnits,
    UnitsPerHour,
    ChangeoverCount,
    ChangeoverHoursMonth,
    AvailableHours,
    MaxCapacityUnits,
    UtilizationPercent,
    DeltaCapacityUnits
}

public sealed record SmtCapacityTableFilter(
    string? Week = null,
    string? Month = null,
    string? Line = null,
    string? ProductFamily = null,
    string? SubFamily = null,
    string? BusinessUnit = null,
    string? LineDistribution = null,
    string? WeeklyDemandUnits = null,
    string? MonthlyMps = null,
    string? AssignedDemandUnits = null,
    string? UnitsPerHour = null,
    string? ChangeoverCount = null,
    string? ChangeoverHoursMonth = null,
    string? AvailableHours = null,
    string? MaxCapacityUnits = null,
    string? UtilizationPercent = null,
    string? DeltaCapacityUnits = null)
{
    public static SmtCapacityTableFilter Empty { get; } = new();
}

public enum SmtCapacityFamilyLineSortColumn
{
    ProductFamily,
    SubFamily,
    BusinessUnit,
    LineDistribution,
    UnitsPerHour,
    ChangeoverCount,
    ChangeoverHoursMonth,
    WeekUtilizationPercent
}

public sealed record SmtCapacityFamilyLineTableFilter(
    string? ProductFamily = null,
    string? SubFamily = null,
    string? BusinessUnit = null,
    string? LineDistribution = null,
    string? UnitsPerHour = null,
    string? ChangeoverCount = null,
    string? ChangeoverHoursMonth = null)
{
    public static SmtCapacityFamilyLineTableFilter Empty { get; } = new();
}

public static class SmtCapacityTableQuery
{
    public static IReadOnlyList<SmtCapacityReportRow> Apply(
        IEnumerable<SmtCapacityReportRow> rows,
        SmtCapacityTableFilter filter,
        SmtCapacitySortColumn sortColumn,
        bool descending)
    {
        var filtered = rows
            .Where(row => Contains(row.WeekName, filter.Week))
            .Where(row => Contains(row.MonthName, filter.Month))
            .Where(row => Contains(row.LineCode, filter.Line))
            .Where(row => Contains(row.ProductFamily, filter.ProductFamily))
            .Where(row => Contains(row.SubFamily, filter.SubFamily))
            .Where(row => Contains(row.BusinessUnit, filter.BusinessUnit))
            .Where(row => ContainsDecimal(row.LineDistribution, filter.LineDistribution, multiplier: 100m))
            .Where(row => ContainsDecimal(row.WeeklyDemandUnits, filter.WeeklyDemandUnits))
            .Where(row => ContainsDecimal(row.MonthlyMps, filter.MonthlyMps))
            .Where(row => ContainsDecimal(row.AssignedDemandUnits, filter.AssignedDemandUnits))
            .Where(row => ContainsDecimal(row.UnitsPerHour, filter.UnitsPerHour))
            .Where(row => ContainsDecimal(row.ChangeoverCount, filter.ChangeoverCount))
            .Where(row => ContainsDecimal(row.ChangeoverHoursMonth, filter.ChangeoverHoursMonth))
            .Where(row => ContainsDecimal(row.AvailableHours, filter.AvailableHours))
            .Where(row => ContainsDecimal(row.MaxCapacityUnits, filter.MaxCapacityUnits))
            .Where(row => ContainsDecimal(row.UtilizationPercent, filter.UtilizationPercent))
            .Where(row => ContainsDecimal(row.DeltaCapacityUnits, filter.DeltaCapacityUnits));

        return Sort(filtered, sortColumn, descending).ToList();
    }

    public static IReadOnlyList<SmtCapacityFamilyLineRow> ApplyFamilyRows(
        IEnumerable<SmtCapacityFamilyLineRow> rows,
        SmtCapacityFamilyLineTableFilter filter,
        SmtCapacityFamilyLineSortColumn sortColumn,
        bool descending,
        string? weekName = null)
    {
        var filtered = rows
            .Where(row => Contains(row.ProductFamily, filter.ProductFamily))
            .Where(row => Contains(row.SubFamily, filter.SubFamily))
            .Where(row => Contains(row.BusinessUnit, filter.BusinessUnit))
            .Where(row => ContainsDecimal(row.LineDistribution, filter.LineDistribution, multiplier: 100m))
            .Where(row => ContainsDecimal(row.UnitsPerHour, filter.UnitsPerHour))
            .Where(row => ContainsDecimal(row.ChangeoverCount, filter.ChangeoverCount))
            .Where(row => ContainsDecimal(row.ChangeoverHoursMonth, filter.ChangeoverHoursMonth));

        return SortFamilyRows(filtered, sortColumn, descending, weekName).ToList();
    }

    private static IOrderedEnumerable<SmtCapacityReportRow> Sort(
        IEnumerable<SmtCapacityReportRow> rows,
        SmtCapacitySortColumn sortColumn,
        bool descending) =>
        sortColumn switch
        {
            SmtCapacitySortColumn.WeekName => SortBy(
                rows,
                row => row.MonthSequence,
                row => row.WeekName,
                descending),
            SmtCapacitySortColumn.MonthName => SortBy(rows, row => row.MonthName ?? string.Empty, descending),
            SmtCapacitySortColumn.LineCode => SortBy(
                rows,
                row => row.LineNumber,
                row => row.LineCode,
                descending),
            SmtCapacitySortColumn.ProductFamily => SortBy(rows, row => row.ProductFamily, descending),
            SmtCapacitySortColumn.SubFamily => SortBy(rows, row => row.SubFamily ?? string.Empty, descending),
            SmtCapacitySortColumn.BusinessUnit => SortBy(rows, row => row.BusinessUnit ?? string.Empty, descending),
            SmtCapacitySortColumn.LineDistribution => SortBy(rows, row => row.LineDistribution, descending),
            SmtCapacitySortColumn.WeeklyDemandUnits => SortBy(rows, row => row.WeeklyDemandUnits, descending),
            SmtCapacitySortColumn.MonthlyMps => SortBy(rows, row => row.MonthlyMps, descending),
            SmtCapacitySortColumn.AssignedDemandUnits => SortBy(rows, row => row.AssignedDemandUnits, descending),
            SmtCapacitySortColumn.UnitsPerHour => SortBy(rows, row => row.UnitsPerHour, descending),
            SmtCapacitySortColumn.ChangeoverCount => SortBy(rows, row => row.ChangeoverCount, descending),
            SmtCapacitySortColumn.ChangeoverHoursMonth => SortBy(rows, row => row.ChangeoverHoursMonth, descending),
            SmtCapacitySortColumn.AvailableHours => SortBy(rows, row => row.AvailableHours, descending),
            SmtCapacitySortColumn.MaxCapacityUnits => SortBy(rows, row => row.MaxCapacityUnits, descending),
            SmtCapacitySortColumn.UtilizationPercent => SortBy(rows, row => row.UtilizationPercent, descending),
            SmtCapacitySortColumn.DeltaCapacityUnits => SortBy(rows, row => row.DeltaCapacityUnits, descending),
            _ => SortBy(rows, row => row.ProductFamily, descending)
        };

    private static IOrderedEnumerable<SmtCapacityFamilyLineRow> SortFamilyRows(
        IEnumerable<SmtCapacityFamilyLineRow> rows,
        SmtCapacityFamilyLineSortColumn sortColumn,
        bool descending,
        string? weekName) =>
        sortColumn switch
        {
            SmtCapacityFamilyLineSortColumn.ProductFamily => SortFamilyRowsBy(rows, row => row.ProductFamily, descending),
            SmtCapacityFamilyLineSortColumn.SubFamily => SortFamilyRowsBy(rows, row => row.SubFamily ?? string.Empty, descending),
            SmtCapacityFamilyLineSortColumn.BusinessUnit => SortFamilyRowsBy(rows, row => row.BusinessUnit ?? string.Empty, descending),
            SmtCapacityFamilyLineSortColumn.LineDistribution => SortFamilyRowsBy(rows, row => row.LineDistribution, descending),
            SmtCapacityFamilyLineSortColumn.UnitsPerHour => SortFamilyRowsBy(rows, row => row.UnitsPerHour, descending),
            SmtCapacityFamilyLineSortColumn.ChangeoverCount => SortFamilyRowsBy(rows, row => row.ChangeoverCount, descending),
            SmtCapacityFamilyLineSortColumn.ChangeoverHoursMonth => SortFamilyRowsBy(rows, row => row.ChangeoverHoursMonth, descending),
            SmtCapacityFamilyLineSortColumn.WeekUtilizationPercent => SortFamilyRowsBy(
                rows,
                row => GetWeekCell(row, weekName)?.UtilizationPercent ?? 0m,
                descending),
            _ => SortFamilyRowsBy(rows, row => row.ProductFamily, descending)
        };

    private static IOrderedEnumerable<SmtCapacityReportRow> SortBy<TKey>(
        IEnumerable<SmtCapacityReportRow> rows,
        Func<SmtCapacityReportRow, TKey> selector,
        bool descending) =>
        descending
            ? rows.OrderByDescending(selector).ThenBy(row => row.ProductFamily)
            : rows.OrderBy(selector).ThenBy(row => row.ProductFamily);

    private static IOrderedEnumerable<SmtCapacityReportRow> SortBy(
        IEnumerable<SmtCapacityReportRow> rows,
        Func<SmtCapacityReportRow, string> selector,
        bool descending) =>
        descending
            ? rows.OrderByDescending(selector, StringComparer.OrdinalIgnoreCase).ThenBy(row => row.ProductFamily)
            : rows.OrderBy(selector, StringComparer.OrdinalIgnoreCase).ThenBy(row => row.ProductFamily);

    private static IOrderedEnumerable<SmtCapacityReportRow> SortBy(
        IEnumerable<SmtCapacityReportRow> rows,
        Func<SmtCapacityReportRow, int> primarySelector,
        Func<SmtCapacityReportRow, string> secondarySelector,
        bool descending) =>
        descending
            ? rows.OrderByDescending(primarySelector)
                .ThenByDescending(secondarySelector, StringComparer.OrdinalIgnoreCase)
                .ThenBy(row => row.ProductFamily)
            : rows.OrderBy(primarySelector)
                .ThenBy(secondarySelector, StringComparer.OrdinalIgnoreCase)
                .ThenBy(row => row.ProductFamily);

    private static IOrderedEnumerable<SmtCapacityFamilyLineRow> SortFamilyRowsBy<TKey>(
        IEnumerable<SmtCapacityFamilyLineRow> rows,
        Func<SmtCapacityFamilyLineRow, TKey> selector,
        bool descending) =>
        descending
            ? rows.OrderByDescending(selector).ThenBy(row => row.ProductFamily)
            : rows.OrderBy(selector).ThenBy(row => row.ProductFamily);

    private static IOrderedEnumerable<SmtCapacityFamilyLineRow> SortFamilyRowsBy(
        IEnumerable<SmtCapacityFamilyLineRow> rows,
        Func<SmtCapacityFamilyLineRow, string> selector,
        bool descending) =>
        descending
            ? rows.OrderByDescending(selector, StringComparer.OrdinalIgnoreCase).ThenBy(row => row.ProductFamily)
            : rows.OrderBy(selector, StringComparer.OrdinalIgnoreCase).ThenBy(row => row.ProductFamily);

    private static bool Contains(string? value, string? filter) =>
        string.IsNullOrWhiteSpace(filter) ||
        (value ?? string.Empty).Contains(filter.Trim(), StringComparison.OrdinalIgnoreCase);

    private static bool ContainsDecimal(decimal value, string? filter, decimal multiplier = 1m)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return true;
        }

        var normalizedFilter = filter.Trim().Replace(',', '.');
        var displayValue = (value * multiplier).ToString("0.###", CultureInfo.InvariantCulture);
        return displayValue.Contains(normalizedFilter, StringComparison.OrdinalIgnoreCase);
    }

    private static SmtCapacityWeekCell? GetWeekCell(SmtCapacityFamilyLineRow row, string? weekName)
    {
        if (string.IsNullOrWhiteSpace(weekName))
        {
            return null;
        }

        return row.WeekCells.TryGetValue(weekName, out var cell) ? cell : null;
    }
}
