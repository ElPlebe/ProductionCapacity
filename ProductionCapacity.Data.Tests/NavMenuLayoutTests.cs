namespace ProductionCapacity.Data.Tests;

public sealed class NavMenuLayoutTests
{
    [Fact]
    public void NavLinksUseSingleLineFlexibleLayout()
    {
        var cssPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "ProductionCapacity",
            "Components",
            "Layout",
            "NavMenu.razor.css"));
        var css = File.ReadAllText(cssPath);

        Assert.Contains("white-space: nowrap;", css);
        Assert.Contains("min-height: 2.75rem;", css);
        Assert.DoesNotContain("height: 3rem;", css);
        Assert.DoesNotContain("line-height: 3rem;", css);
    }
}
