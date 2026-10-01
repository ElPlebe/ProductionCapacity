namespace ProductionCapacity.Data.Tests;

public sealed class SmtAdministrationMarkupTests
{
    [Fact]
    public void PageIncludesSearchSortableHeadersAndCancelActions()
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
            "SmtAdministration.razor"));
        var markup = File.ReadAllText(markupPath);

        Assert.Contains("Buscar en catalogo", markup);
        Assert.Contains("SortableFamilyHeader", markup);
        Assert.Contains("SortableFamilyRateHeader", markup);
        Assert.Contains("SortableLineRateHeader", markup);
        Assert.Contains("Cancelar edicion", markup);
        Assert.Contains("Historial SMT", markup);
        Assert.Contains("Snapshot.AuditLogs", markup);
        Assert.Contains("Distribucion por linea", markup);
        Assert.Contains("Calendario SMT", markup);
        Assert.Contains("SaveLineAssignmentAsync", markup);
        Assert.Contains("SaveOperationCapacityAsync", markup);
        Assert.Contains("Targets Capacity SMT", markup);
        Assert.Contains("SaveCapacityTargetAsync", markup);
        Assert.Contains("SortableLineAssignmentHeader", markup);
        Assert.Contains("SortableOperationCapacityHeader", markup);
        Assert.Contains("SortableCapacityTargetHeader", markup);
        Assert.Contains("SortableAuditHeader", markup);
    }
}
