namespace ProductionCapacity.Data.Tests;

public sealed class ImportExcelMarkupTests
{
    [Fact]
    public void DatabaseSnapshotShowsSmtFamilyCatalogCount()
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
            "ImportExcel.razor"));
        var markup = File.ReadAllText(markupPath);

        Assert.Contains("Familias SMT", markup);
        Assert.Contains("DatabaseSnapshot.SmtFamilyCount", markup);
    }

    [Fact]
    public void ImportPreviewShowsSmtImpactSections()
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
            "ImportExcel.razor"));
        var markup = File.ReadAllText(markupPath);

        Assert.Contains("Revision SMT antes de guardar", markup);
        Assert.Contains("Familias nuevas", markup);
        Assert.Contains("UPH que se reemplaza", markup);
        Assert.Contains("Datos incompletos", markup);
    }
}
