using System.Text.RegularExpressions;

namespace ProductionCapacity.Data.Tests;

public sealed partial class SmtCapacityMarkupTests
{
    [Fact]
    public void HomePageIsSmtSummaryScreen()
    {
        var markupPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "ProductionCapacity",
            "Components",
            "Pages",
            "Home.razor"));
        var markup = File.ReadAllText(markupPath);

        Assert.Contains("Summary SMT", markup);
        Assert.Contains("Linearity", markup);
        Assert.Contains("BUILD PROJECTION", markup);
        Assert.Contains("SCR Commit", markup);
        Assert.Contains("SMT CAPACITY", markup);
        Assert.Contains("SmtSummaryService", markup);
    }

    [Fact]
    public void NavigationShowsSummaryAsTheHomeTab()
    {
        var markupPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "ProductionCapacity",
            "Components",
            "Layout",
            "NavMenu.razor"));
        var markup = File.ReadAllText(markupPath);

        Assert.Contains("> Summary", markup);
        Assert.DoesNotContain("> Planeacion", markup);
    }

    [Fact]
    public void SortIndicatorCallsAreSeparatedFromHeaderText()
    {
        var markupPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "ProductionCapacity",
            "Components",
            "Pages",
            "SmtCapacity.razor"));
        var markup = File.ReadAllText(markupPath);

        Assert.DoesNotMatch(RenderedLiteralSortIndicatorPattern(), markup);
    }

    [Fact]
    public void CapacityParityTableHasFiltersAndClickableHeaders()
    {
        var markupPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "ProductionCapacity",
            "Components",
            "Pages",
            "SmtCapacity.razor"));
        var markup = File.ReadAllText(markupPath);

        Assert.Contains("Comparacion Capacity SMT", markup);
        Assert.Contains("ParityRows", markup);
        Assert.Contains("ToggleParitySort", markup);
        Assert.Contains("ParityOnlyDifferences", markup);
        Assert.Contains("ParityFamilyFilter", markup);
    }

    [Fact]
    public void SummaryCapacityUsesWholeNumberRoundUpFormatter()
    {
        var markupPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "ProductionCapacity",
            "Components",
            "Pages",
            "SmtCapacity.razor"));
        var markup = File.ReadAllText(markupPath);

        Assert.Contains("Cap @FormatSmtCapacity(summary.MaxCapacityUnits)", markup);
        Assert.Contains("DecimalRounding.FormatUp(value, decimals: 0)", markup);
    }

    [Fact]
    public void CapacityScreenHasInlineAssignmentEditorAndRequestedLoadThresholds()
    {
        var markupPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "ProductionCapacity",
            "Components",
            "Pages",
            "SmtCapacity.razor"));
        var markup = File.ReadAllText(markupPath);

        Assert.Contains("Editar asignacion", markup);
        Assert.Contains("SaveAssignmentEditAsync", markup);
        Assert.Contains("loadPercent > 99.4m", markup);
        Assert.Contains("loadPercent >= 85.5m", markup);
    }

    [Fact]
    public void CapacityScreenSupportsExcelStyleSummaryEditingAndVolumeSource()
    {
        var markupPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "ProductionCapacity",
            "Components",
            "Pages",
            "SmtCapacity.razor"));
        var markup = File.ReadAllText(markupPath);

        Assert.Contains("Fuente de volumen (MPS)", markup);
        Assert.Contains("VolumeRows", markup);
        Assert.Contains("StartSummaryAssignmentEdit", markup);
        Assert.Contains("Impacto estimado", markup);
        Assert.Contains("Volumen = MPS", markup);
        Assert.Contains("Cap = capacidad calculada", markup);
    }

    [Fact]
    public void SummaryTableShowsBusinessUnitBeforeFamily()
    {
        var markupPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "ProductionCapacity",
            "Components",
            "Pages",
            "SmtCapacity.razor"));
        var markup = File.ReadAllText(markupPath);
        var summaryHeaderStart = markup.IndexOf("ToggleSummarySort(SmtCapacityFamilyLineSortColumn.BusinessUnit)", StringComparison.Ordinal);
        var familyHeaderStart = markup.IndexOf("ToggleSummarySort(SmtCapacityFamilyLineSortColumn.ProductFamily)", StringComparison.Ordinal);
        var subFamilyHeaderStart = markup.IndexOf("ToggleSummarySort(SmtCapacityFamilyLineSortColumn.SubFamily)", StringComparison.Ordinal);

        Assert.True(summaryHeaderStart >= 0, "The summary table should include a BU header.");
        Assert.True(familyHeaderStart >= 0, "The summary table should include a Familia header.");
        Assert.True(subFamilyHeaderStart >= 0, "The summary table should include a Subfamilia header.");
        Assert.True(summaryHeaderStart < familyHeaderStart, "BU should appear before Familia in the summary table.");
        Assert.True(familyHeaderStart < subFamilyHeaderStart, "Familia should appear before Subfamilia in the summary table.");
    }

    [GeneratedRegex(@"[A-Za-z0-9_.%)]@(?:SummarySortIndicator|SummaryWeekSortIndicator|SortIndicator)\(")]
    private static partial Regex RenderedLiteralSortIndicatorPattern();
}
