namespace ProductionCapacity.Data.Tests;

public sealed class SmtCapacityFamilyLineTableQueryTests
{
    [Fact]
    public void FiltersFamilyRowsByTextColumns()
    {
        var rows = new[]
        {
            Row("AZTECA", "FC3", "ERBU", unitsPerHour: 2.18m, utilizationPercent: 10m),
            Row("BLACK BOLT", "MAIN", "NGFWBU", unitsPerHour: 3.5m, utilizationPercent: 20m),
            Row("Durlast", "FC3", "ERBU", unitsPerHour: 1.8m, utilizationPercent: 30m)
        };

        var filter = new SmtCapacityFamilyLineTableFilter(SubFamily: "fc3", BusinessUnit: "erbu");

        var filtered = SmtCapacityTableQuery.ApplyFamilyRows(
            rows,
            filter,
            SmtCapacityFamilyLineSortColumn.ProductFamily,
            descending: false).ToList();

        Assert.Collection(
            filtered,
            first => Assert.Equal("AZTECA", first.ProductFamily),
            second => Assert.Equal("Durlast", second.ProductFamily));
    }

    [Fact]
    public void SortsFamilyRowsByNumericColumns()
    {
        var rows = new[]
        {
            Row("A", "FC3", "ERBU", unitsPerHour: 2.18m, utilizationPercent: 10m),
            Row("B", "MAIN", "ERBU", unitsPerHour: 4.25m, utilizationPercent: 20m),
            Row("C", "WIRE", "ERBU", unitsPerHour: 3.10m, utilizationPercent: 30m)
        };

        var sorted = SmtCapacityTableQuery.ApplyFamilyRows(
            rows,
            SmtCapacityFamilyLineTableFilter.Empty,
            SmtCapacityFamilyLineSortColumn.UnitsPerHour,
            descending: true).ToList();

        Assert.Collection(
            sorted,
            first => Assert.Equal("B", first.ProductFamily),
            second => Assert.Equal("C", second.ProductFamily),
            third => Assert.Equal("A", third.ProductFamily));
    }

    [Fact]
    public void SortsFamilyRowsBySelectedWeekUtilization()
    {
        var rows = new[]
        {
            Row("A", "FC3", "ERBU", unitsPerHour: 2.18m, utilizationPercent: 10m),
            Row("B", "MAIN", "ERBU", unitsPerHour: 4.25m, utilizationPercent: 40m),
            Row("C", "WIRE", "ERBU", unitsPerHour: 3.10m, utilizationPercent: 25m)
        };

        var sorted = SmtCapacityTableQuery.ApplyFamilyRows(
            rows,
            SmtCapacityFamilyLineTableFilter.Empty,
            SmtCapacityFamilyLineSortColumn.WeekUtilizationPercent,
            descending: true,
            weekName: "M1").ToList();

        Assert.Collection(
            sorted,
            first => Assert.Equal("B", first.ProductFamily),
            second => Assert.Equal("C", second.ProductFamily),
            third => Assert.Equal("A", third.ProductFamily));
    }

    private static SmtCapacityFamilyLineRow Row(
        string family,
        string subFamily,
        string businessUnit,
        decimal unitsPerHour,
        decimal utilizationPercent) =>
        new(
            "SMT",
            "SMT 1",
            1,
            family,
            subFamily,
            businessUnit,
            1m,
            unitsPerHour,
            0m,
            0m,
            509.4m,
            new Dictionary<string, SmtCapacityWeekCell>
            {
                ["M1"] = new(1m, 4m, 4m, 100m, utilizationPercent, 96m)
            });
}
