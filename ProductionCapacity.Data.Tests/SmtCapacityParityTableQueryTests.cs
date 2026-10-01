using ProductionCapacity.Data;

namespace ProductionCapacity.Data.Tests;

public sealed class SmtCapacityParityTableQueryTests
{
    [Fact]
    public void ApplyCanFilterOnlyDifferencesAndText()
    {
        var rows = CreateRows();

        var result = SmtCapacityParityTableQuery.Apply(
            rows,
            new SmtCapacityParityTableFilter(ProductFamily: "az"),
            SmtCapacityParitySortColumn.WeekName,
            descending: false,
            onlyDifferences: true);

        var row = Assert.Single(result);
        Assert.Equal("AZTECA", row.ProductFamily);
        Assert.False(row.IsPassing);
    }

    [Fact]
    public void ApplySortsNumericColumns()
    {
        var rows = CreateRows();

        var result = SmtCapacityParityTableQuery.Apply(
            rows,
            SmtCapacityParityTableFilter.Empty,
            SmtCapacityParitySortColumn.ActualUtilizationPercent,
            descending: true,
            onlyDifferences: false);

        Assert.Equal(["BLACK BOLT", "AZTECA", "DRACO"], result.Select(row => row.ProductFamily));
    }

    private static IReadOnlyList<SmtCapacityParityRow> CreateRows() =>
    [
        new(
            "M1",
            "SMT",
            "SMT 1",
            "AZTECA",
            100m,
            110m,
            1000m,
            1000m,
            10m,
            11m,
            900m,
            890m,
            "Capacity SMT!A1",
            "Diferencia contra Capacity SMT fuera de tolerancia.",
            false),
        new(
            "M2",
            "SMT",
            "SMT 2",
            "BLACK BOLT",
            100m,
            100m,
            1000m,
            1000m,
            20m,
            20m,
            900m,
            900m,
            "Capacity SMT!A2",
            null,
            true),
        new(
            "M3",
            "SMT",
            "SMT 3",
            "DRACO",
            100m,
            90m,
            1000m,
            1000m,
            5m,
            9m,
            900m,
            910m,
            "Capacity SMT!A3",
            "Diferencia contra Capacity SMT fuera de tolerancia.",
            false)
    ];
}
