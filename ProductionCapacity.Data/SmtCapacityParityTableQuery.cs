using System.Globalization;

namespace ProductionCapacity.Data;

public enum SmtCapacityParitySortColumn
{
    WeekName,
    OperationCode,
    LineCode,
    ProductFamily,
    ExpectedAssignedDemandUnits,
    ActualAssignedDemandUnits,
    ExpectedMaxCapacityUnits,
    ActualMaxCapacityUnits,
    ExpectedUtilizationPercent,
    ActualUtilizationPercent,
    ExpectedDeltaCapacityUnits,
    ActualDeltaCapacityUnits,
    SourceReference,
    Status
}

public sealed record SmtCapacityParityTableFilter(
    string? Week = null,
    string? Operation = null,
    string? Line = null,
    string? ProductFamily = null,
    string? ExpectedAssignedDemandUnits = null,
    string? ActualAssignedDemandUnits = null,
    string? ExpectedMaxCapacityUnits = null,
    string? ActualMaxCapacityUnits = null,
    string? ExpectedUtilizationPercent = null,
    string? ActualUtilizationPercent = null,
    string? ExpectedDeltaCapacityUnits = null,
    string? ActualDeltaCapacityUnits = null,
    string? SourceReference = null,
    string? Status = null)
{
    public static SmtCapacityParityTableFilter Empty { get; } = new();
}

public static class SmtCapacityParityTableQuery
{
    public static IReadOnlyList<SmtCapacityParityRow> Apply(
        IEnumerable<SmtCapacityParityRow> rows,
        SmtCapacityParityTableFilter filter,
        SmtCapacityParitySortColumn sortColumn,
        bool descending,
        bool onlyDifferences)
    {
        var filtered = rows
            .Where(row => !onlyDifferences || !row.IsPassing)
            .Where(row => Contains(row.WeekName, filter.Week))
            .Where(row => Contains(row.OperationCode, filter.Operation))
            .Where(row => Contains(row.LineCode, filter.Line))
            .Where(row => Contains(row.ProductFamily, filter.ProductFamily))
            .Where(row => ContainsDecimal(row.ExpectedAssignedDemandUnits, filter.ExpectedAssignedDemandUnits))
            .Where(row => ContainsDecimal(row.ActualAssignedDemandUnits, filter.ActualAssignedDemandUnits))
            .Where(row => ContainsDecimal(row.ExpectedMaxCapacityUnits, filter.ExpectedMaxCapacityUnits))
            .Where(row => ContainsDecimal(row.ActualMaxCapacityUnits, filter.ActualMaxCapacityUnits))
            .Where(row => ContainsDecimal(row.ExpectedUtilizationPercent, filter.ExpectedUtilizationPercent))
            .Where(row => ContainsDecimal(row.ActualUtilizationPercent, filter.ActualUtilizationPercent))
            .Where(row => ContainsDecimal(row.ExpectedDeltaCapacityUnits, filter.ExpectedDeltaCapacityUnits))
            .Where(row => ContainsDecimal(row.ActualDeltaCapacityUnits, filter.ActualDeltaCapacityUnits))
            .Where(row => Contains(row.SourceReference, filter.SourceReference))
            .Where(row => Contains(GetStatus(row), filter.Status));

        return Sort(filtered, sortColumn, descending).ToList();
    }

    private static IOrderedEnumerable<SmtCapacityParityRow> Sort(
        IEnumerable<SmtCapacityParityRow> rows,
        SmtCapacityParitySortColumn sortColumn,
        bool descending) =>
        sortColumn switch
        {
            SmtCapacityParitySortColumn.WeekName => SortBy(rows, row => row.WeekName, descending),
            SmtCapacityParitySortColumn.OperationCode => SortBy(rows, row => row.OperationCode, descending),
            SmtCapacityParitySortColumn.LineCode => SortBy(rows, row => row.LineCode, descending),
            SmtCapacityParitySortColumn.ProductFamily => SortBy(rows, row => row.ProductFamily, descending),
            SmtCapacityParitySortColumn.ExpectedAssignedDemandUnits => SortBy(rows, row => row.ExpectedAssignedDemandUnits ?? decimal.MinValue, descending),
            SmtCapacityParitySortColumn.ActualAssignedDemandUnits => SortBy(rows, row => row.ActualAssignedDemandUnits ?? decimal.MinValue, descending),
            SmtCapacityParitySortColumn.ExpectedMaxCapacityUnits => SortBy(rows, row => row.ExpectedMaxCapacityUnits ?? decimal.MinValue, descending),
            SmtCapacityParitySortColumn.ActualMaxCapacityUnits => SortBy(rows, row => row.ActualMaxCapacityUnits ?? decimal.MinValue, descending),
            SmtCapacityParitySortColumn.ExpectedUtilizationPercent => SortBy(rows, row => row.ExpectedUtilizationPercent ?? decimal.MinValue, descending),
            SmtCapacityParitySortColumn.ActualUtilizationPercent => SortBy(rows, row => row.ActualUtilizationPercent ?? decimal.MinValue, descending),
            SmtCapacityParitySortColumn.ExpectedDeltaCapacityUnits => SortBy(rows, row => row.ExpectedDeltaCapacityUnits ?? decimal.MinValue, descending),
            SmtCapacityParitySortColumn.ActualDeltaCapacityUnits => SortBy(rows, row => row.ActualDeltaCapacityUnits ?? decimal.MinValue, descending),
            SmtCapacityParitySortColumn.SourceReference => SortBy(rows, row => row.SourceReference ?? string.Empty, descending),
            SmtCapacityParitySortColumn.Status => SortBy(rows, GetStatus, descending),
            _ => SortBy(rows, row => row.WeekName, descending)
        };

    private static IOrderedEnumerable<SmtCapacityParityRow> SortBy<TKey>(
        IEnumerable<SmtCapacityParityRow> rows,
        Func<SmtCapacityParityRow, TKey> selector,
        bool descending) =>
        descending
            ? rows.OrderByDescending(selector).ThenBy(row => row.WeekName).ThenBy(row => row.LineCode).ThenBy(row => row.ProductFamily)
            : rows.OrderBy(selector).ThenBy(row => row.WeekName).ThenBy(row => row.LineCode).ThenBy(row => row.ProductFamily);

    private static IOrderedEnumerable<SmtCapacityParityRow> SortBy(
        IEnumerable<SmtCapacityParityRow> rows,
        Func<SmtCapacityParityRow, string> selector,
        bool descending) =>
        descending
            ? rows.OrderByDescending(selector, StringComparer.OrdinalIgnoreCase).ThenBy(row => row.WeekName).ThenBy(row => row.LineCode).ThenBy(row => row.ProductFamily)
            : rows.OrderBy(selector, StringComparer.OrdinalIgnoreCase).ThenBy(row => row.WeekName).ThenBy(row => row.LineCode).ThenBy(row => row.ProductFamily);

    private static bool Contains(string? value, string? filter) =>
        string.IsNullOrWhiteSpace(filter) ||
        (value ?? string.Empty).Contains(filter.Trim(), StringComparison.OrdinalIgnoreCase);

    private static bool ContainsDecimal(decimal? value, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return true;
        }

        var normalizedFilter = filter.Trim().Replace(',', '.');
        var displayValue = value.HasValue
            ? value.Value.ToString("0.###", CultureInfo.InvariantCulture)
            : "-";
        return displayValue.Contains(normalizedFilter, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetStatus(SmtCapacityParityRow row) =>
        row.IsPassing ? "OK" : row.Message ?? "Diferencia";
}
