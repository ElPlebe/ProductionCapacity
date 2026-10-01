using System.IO.Compression;
using Microsoft.EntityFrameworkCore;
using ProductionCapacity.Components;
using ProductionCapacity.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddDbContextFactory<PlanningDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ProductionCapacity")));
builder.Services.AddScoped<CapacityPlanService>();
builder.Services.AddScoped<CatalogAdministrationService>();
builder.Services.AddScoped<PlanningMatrixService>();
builder.Services.AddScoped<ParityComparisonService>();
builder.Services.AddScoped<SmtCapacityReportService>();
builder.Services.AddScoped<SmtSummaryService>();
builder.Services.AddScoped<SmtCapacityParityService>();
builder.Services.AddScoped<SmtAdministrationService>();
builder.Services.AddScoped<SmtDiagnosticsService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

var importTemplateDirectory = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "import-templates"));
app.MapGet("/plantillas-importacion/todas.zip", () =>
{
    if (!Directory.Exists(importTemplateDirectory))
    {
        return Results.NotFound("No se encontro la carpeta de plantillas de importacion.");
    }

    using var memory = new MemoryStream();
    using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, leaveOpen: true))
    {
        foreach (var templatePath in Directory.GetFiles(importTemplateDirectory, "*.*")
            .Where(path => path.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            .OrderBy(Path.GetFileName))
        {
            archive.CreateEntryFromFile(templatePath, Path.GetFileName(templatePath));
        }
    }

    return Results.File(
        memory.ToArray(),
        "application/zip",
        "plantillas-importacion-capacidad.zip");
});

app.MapGet("/plantillas-importacion/{fileName}", (string fileName) =>
{
    var safeFileName = Path.GetFileName(fileName);
    var isCsv = safeFileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase);
    var isXlsx = safeFileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase);
    if (!isCsv && !isXlsx)
    {
        return Results.BadRequest("Solo se pueden descargar plantillas CSV o XLSX.");
    }

    var templatePath = Path.GetFullPath(Path.Combine(importTemplateDirectory, safeFileName));
    if (!templatePath.StartsWith(importTemplateDirectory, StringComparison.OrdinalIgnoreCase) ||
        !File.Exists(templatePath))
    {
        return Results.NotFound("La plantilla solicitada no existe.");
    }

    return Results.File(
        templatePath,
        isXlsx
            ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            : "text/csv",
        safeFileName);
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
