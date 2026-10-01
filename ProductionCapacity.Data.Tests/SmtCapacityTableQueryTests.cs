namespace ProductionCapacity.Data.Tests;

public sealed class SmtCapacityTableQueryTests
{
    [Fact]
    public void FiltersRowsByTextColumns()
    {
        var rows = new[]
        {
            Row("M1", "Agosto 2026", "SMT 1", 1, "AZTECA", "FC3", "ERBU", 10m),
            Row("M2", "Septiembre 2026", "SMT 2", 2, "BLACK BOLT", "MAIN", "NGFWBU", 20m),
            Row("M3", "Octubre 2026", "SMT 1", 1, "Durlast", "FC3", "ERBU", 30m)
        };

        var filter = new SmtCapacityTableFilter(Line: "smt 1", SubFamily: "fc3", BusinessUnit: "erbu");

        var filtered = SmtCapacityTableQuery.Apply(
            rows,
            filter,
            SmtCapacitySortColumn.ProductFamily,
            descending: false).ToList();

        Assert.Collection(
            filtered,
            first => Assert.Equal("AZTECA", first.ProductFamily),
            second => Assert.Equal("Durlast", second.ProductFamily));
    }

    [Fact]
    public void SortsTextColumnsAlphabetically()
    {
        var rows = new[]
        {
            Row("M1", "Agosto 2026", "SMT 1", 1, "Durlast", "WIRE", "ERBU", 10m),
            Row("M1", "Agosto 2026", "SMT 1", 1, "AZTECA", "FC3", "ERBU", 10m),
            Row("M1", "Agosto 2026", "SMT 1", 1, "BLACK BOLT", "MAIN", "ERBU", 10m)
        };

        var sorted = SmtCapacityTableQuery.Apply(
            rows,
            SmtCapacityTableFilter.Empty,
            SmtCapacitySortColumn.SubFamily,
            descending: false).ToList();

        Assert.Collection(
            sorted,
            first => Assert.Equal("AZTECA", first.ProductFamily),
            second => Assert.Equal("BLACK BOLT", second.ProductFamily),
            third => Assert.Equal("Durlast", third.ProductFamily));
    }

    [Fact]
    public void SortsNumericColumnsByValue()
    {
        var rows = new[]
        {
            Row("M1", "Agosto 2026", "SMT 1", 1, "A", "FC3", "ERBU", 10m),
            Row("M1", "Agosto 2026", "SMT 1", 1, "B", "MAIN", "ERBU", 30m),
            Row("M1", "Agosto 2026", "SMT 1", 1, "C", "WIRE", "ERBU", 20m)
        };

        var sorted = SmtCapacityTableQuery.Apply(
            rows,
            SmtCapacityTableFilter.Empty,
            SmtCapacitySortColumn.UtilizationPercent,
            descending: true).ToList();

        Assert.Collection(
            sorted,
            first => Assert.Equal("B", first.ProductFamily),
            second => Assert.Equal("C", second.ProductFamily),
            third => Assert.Equal("A", third.ProductFamily));
    }

    [Fact]
    public void FiltersRowsByNumericColumns()
    {
        var rows = new[]
        {
            Row("M1", "Agosto 2026", "SMT 1", 1, "A", "FC3", "ERBU", 10m),
            Row("M1", "Agosto 2026", "SMT 1", 1, "B", "MAIN", "ERBU", 30m),
            Row("M1", "Agosto 2026", "SMT 1", 1, "C", "WIRE", "ERBU", 20m)
        };

        var filter = new SmtCapacityTableFilter(UtilizationPercent: "20");

        var filtered = SmtCapacityTableQuery.Apply(
            rows,
            filter,
            SmtCapacitySortColumn.ProductFamily,
            descending: false).ToList();

        var row = Assert.Single(filtered);
        Assert.Equal("C", row.ProductFamily);
    }

    [Fact]
    public void SortsWeekColumnByMonthSequenceBeforeName()
    {
        var rows = new[]
        {
            Row("M3", "Octubre 2026", "SMT 1", 1, "A", "FC3", "ERBU", 10m, monthSequence: 3),
            Row("M1", "Agosto 2026", "SMT 1", 1, "B", "MAIN", "ERBU", 10m, monthSequence: 1),
            Row("M2", "Septiembre 2026", "SMT 1", 1, "C", "WIRE", "ERBU", 10m, monthSequence: 2)
        };

        var sorted = SmtCapacityTableQuery.Apply(
            rows,
            SmtCapacityTableFilter.Empty,
            SmtCapacitySortColumn.WeekName,
            descending: false).ToList();

        Assert.Collection(
            sorted,
            first => Assert.Equal("M1", first.WeekName),
            second => Assert.Equal("M2", second.WeekName),
            third => Assert.Equal("M3", third.WeekName));
    }

    private static SmtCapacityReportRow Row(
        string weekName,
        string monthName,
        string lineCode,
        int lineNumber,
        string family,
        string subFamily,
        string businessUnit,
        decimal utilizationPercent,
        int monthSequence = 1) =>
        new(
            weekName,
            monthName,
            monthSequence,
            "SMT",
            lineCode,
            lineNumber,
            family,
            subFamily,
            businessUnit,
            1m,
            1m,
            4m,
            4m,
            2.18m,
            0m,
            0m,
            509.4m,
            100m,
            utilizationPercent,
            96m);
}
