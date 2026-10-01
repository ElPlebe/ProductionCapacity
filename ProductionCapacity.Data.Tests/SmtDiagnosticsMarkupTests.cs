namespace ProductionCapacity.Data.Tests;

public sealed class SmtDiagnosticsMarkupTests
{
    [Fact]
    public void PageShowsSmtReadinessSections()
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
            "SmtDiagnostics.razor"));
        var markup = File.ReadAllText(markupPath);

        Assert.Contains("@page \"/diagnostico-smt\"", markup);
        Assert.Contains("Diagnostico SMT", markup);
        Assert.Contains("Semaforo SMT", markup);
        Assert.Contains("Snapshot.Issues", markup);
        Assert.Contains("SmtDiagnosticsService", markup);
    }
}
