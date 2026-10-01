using Microsoft.EntityFrameworkCore;
using ProductionCapacity.Data;
using ProductionCapacity.Data.Entities;
using ProductionCapacity.Data.ExcelImport;

namespace ProductionCapacity.Data.Tests;

public sealed class ExcelCapacityImportServiceSmtTests
{
    [Fact]
    public async Task ApplyCreatesAndUpdatesSmtFamiliesFromImportPackage()
    {
        await using var db = CreateDbContext();
        db.SmtFamilies.Add(new SmtFamilyEntity
        {
            ProductFamily = "AZTECA",
            BusinessUnit = "OLD",
            Notes = "Anterior",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new ExcelCapacityImportService(db);

        await service.ApplyAsync(CreateSmtPackage());

        var families = await db.SmtFamilies.OrderBy(family => family.ProductFamily).ToListAsync();
        Assert.Collection(
            families,
            azteca =>
            {
                Assert.Equal("AZTECA", azteca.ProductFamily);
                Assert.Equal("ERBU", azteca.BusinessUnit);
                Assert.Equal("Importado/actualizado desde paquete SMT.", azteca.Notes);
                Assert.True(azteca.IsActive);
            },
            bronco =>
            {
                Assert.Equal("BRONCO", bronco.ProductFamily);
                Assert.Equal("SRBU", bronco.BusinessUnit);
                Assert.True(bronco.IsActive);
            });
        Assert.Contains(await db.SmtAuditLogs.ToListAsync(), log =>
            log.EntityType == "Familia SMT" &&
            log.EntityKey == "BRONCO" &&
            log.ChangedBy == "Importacion SMT");
        Assert.Contains(await db.SmtAuditLogs.ToListAsync(), log =>
            log.EntityType == "UPH Familia SMT" &&
            log.EntityKey == "AZTECA|SMT" &&
            log.NewValue!.Contains("UPH 2.18", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ApplyCreatesSmtSubFamiliesFromImportedPartNumbers()
    {
        await using var db = CreateDbContext();
        var service = new ExcelCapacityImportService(db);

        await service.ApplyAsync(CreateSmtPackage());

        var subFamilies = await db.SubFamilies
            .OrderBy(item => item.ProductFamily)
            .ThenBy(item => item.SubFamilyName)
            .ToListAsync();

        Assert.Collection(
            subFamilies,
            first =>
            {
                Assert.Equal("AZTECA", first.ProductFamily);
                Assert.Equal("FC3", first.SubFamilyName);
                Assert.True(first.IsActive);
            },
            second =>
            {
                Assert.Equal("BRONCO", second.ProductFamily);
                Assert.Equal("MAIN", second.SubFamilyName);
                Assert.True(second.IsActive);
            });
        Assert.Contains(await db.SmtAuditLogs.ToListAsync(), log =>
            log.EntityType == "Subfamilia SMT" &&
            log.EntityKey == "AZTECA|FC3" &&
            log.ChangedBy == "Importacion SMT");
    }

    [Fact]
    public async Task PreviewWarnsAboutSmtImportGapsBeforeSaving()
    {
        await using var db = CreateDbContext();
        var package = CreateSmtPackage() with
        {
            Operations = [],
            SmtFamilyRates = [],
            SmtLineAssignments =
            [
                new ExcelSmtLineAssignmentImportRow("Base FMD SMT", "M1", "AZTECA", "SMT2", "SMT 1", 1, 0m, "Agosto 2026", 1)
            ]
        };
        var service = new ExcelCapacityImportService(db);

        var preview = await service.PreviewAsync(package);

        Assert.Contains(preview.Warnings, warning => warning.Contains("operacion SMT 'SMT2'", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(preview.Warnings, warning => warning.Contains("familia SMT 'AZTECA'", StringComparison.OrdinalIgnoreCase) && warning.Contains("UPH", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(preview.Warnings, warning => warning.Contains("distribucion", StringComparison.OrdinalIgnoreCase) && warning.Contains("AZTECA", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PreviewWarnsWhenSmtLineDistributionLooksLikeWholePercent()
    {
        await using var db = CreateDbContext();
        var package = CreateSmtPackage() with
        {
            SmtLineAssignments =
            [
                new ExcelSmtLineAssignmentImportRow("Base FMD SMT", "M1", "AZTECA", "SMT", "SMT 1", 1, 100m, "Agosto 2026", 1)
            ]
        };
        var service = new ExcelCapacityImportService(db);

        var preview = await service.PreviewAsync(package);

        Assert.Contains(preview.Warnings, warning =>
            warning.Contains("100%", StringComparison.OrdinalIgnoreCase) &&
            warning.Contains("AZTECA", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PreviewWarnsWhenSmtPartNumberHasNoSubFamily()
    {
        await using var db = CreateDbContext();
        var package = CreateSmtPackage() with
        {
            PartNumbers =
            [
                new ExcelPartNumberImportRow("68-100", "AZTECA 68-100", "ERBU", "AZTECA", 1m)
            ]
        };
        var service = new ExcelCapacityImportService(db);

        var preview = await service.PreviewAsync(package);

        Assert.Contains(preview.Warnings, warning =>
            warning.Contains("subfamilia", StringComparison.OrdinalIgnoreCase) &&
            warning.Contains("68-100", StringComparison.OrdinalIgnoreCase));
    }


    [Fact]
    public async Task PreviewSummarizesSmtImportImpact()
    {
        await using var db = CreateDbContext();
        db.SmtFamilies.Add(new SmtFamilyEntity
        {
            ProductFamily = "AZTECA",
            BusinessUnit = "ERBU",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        db.Operations.Add(new PlanningOperation
        {
            OperationId = 1,
            OperationCode = "SMT",
            OperationName = "Surface Mount",
            OperationFamily = "SMT",
            SequenceNumber = 1,
            IsActive = true
        });
        db.SmtFamilyRates.Add(new SmtFamilyRateEntity
        {
            ProductFamily = "AZTECA",
            OperationId = 1,
            UnitsPerHour = 1.50m,
            ChangeoverCount = 1m,
            IsActive = true
        });
        await db.SaveChangesAsync();
        var service = new ExcelCapacityImportService(db);

        var preview = await service.PreviewAsync(CreateSmtPackage());

        Assert.Contains(preview.SmtImpact.NewFamilies, item => item.ProductFamily == "BRONCO");
        Assert.Contains(preview.SmtImpact.ExistingFamilies, item => item.ProductFamily == "AZTECA");
        var replacement = Assert.Single(preview.SmtImpact.FamilyRateReplacements);
        Assert.Equal("AZTECA", replacement.ProductFamily);
        Assert.Equal("SMT", replacement.OperationCode);
        Assert.Equal(1.50m, replacement.CurrentUnitsPerHour);
        Assert.Equal(2.18m, replacement.ImportedUnitsPerHour);
        Assert.Empty(preview.SmtImpact.IncompleteItems);
    }

    private static PlanningDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PlanningDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new PlanningDbContext(options);
    }

    private static ExcelCapacityImportPackage CreateSmtPackage() => new(
        Scenarios:
        [
            new ExcelScenarioImportRow("Base FMD SMT", "Escenario SMT", true)
        ],
        ProductionWeeks:
        [
            new ExcelProductionWeekImportRow("Base FMD SMT", "M1", null, 5.66m, 7.5m, 3, 0m, 0m, 0m)
        ],
        PartNumbers:
        [
            new ExcelPartNumberImportRow("68-100", "AZTECA 68-100", "ERBU", "AZTECA", 1m, "FC3"),
            new ExcelPartNumberImportRow("68-200", "BRONCO 68-200", "SRBU", "BRONCO", 1m, "MAIN")
        ],
        WeeklyDemands:
        [
            new ExcelWeeklyDemandImportRow("Base FMD SMT", "M1", null, "AZTECA", 25m),
            new ExcelWeeklyDemandImportRow("Base FMD SMT", "M1", null, "BRONCO", 10m)
        ],
        Operations:
        [
            new ExcelOperationImportRow("SMT", "Surface Mount", "SMT", 1)
        ],
        OperationRates: [],
        OperationCapacities:
        [
            new ExcelOperationCapacityImportRow("Base FMD SMT", "M1", "SMT", 5.66m, 7.5m, 3, 6m, 0m, 0m, 0m)
        ],
        CrossReferences: [],
        TestTimes: [],
        ResourceTypes: [],
        ResourceConfigurations: [],
        ResourceInventories: [],
        SmtFamilyVolumes:
        [
            new ExcelSmtFamilyVolumeImportRow("Base FMD SMT", "M1", "ERBU", "AZTECA", "Agosto 2026", 1, 100m, 25m),
            new ExcelSmtFamilyVolumeImportRow("Base FMD SMT", "M1", "SRBU", "BRONCO", "Agosto 2026", 1, 40m, 10m)
        ],
        SmtFamilyRates:
        [
            new ExcelSmtFamilyRateImportRow("AZTECA", "SMT", 2.18m, 1m, "68-100", 2.18m, null, null, null),
            new ExcelSmtFamilyRateImportRow("BRONCO", "SMT", 3.25m, 1m, "68-200", 3.25m, null, null, null)
        ],
        SmtLineFamilyRates: [],
        SmtLineAssignments:
        [
            new ExcelSmtLineAssignmentImportRow("Base FMD SMT", "M1", "AZTECA", "SMT", "SMT 1", 1, 1m, "Agosto 2026", 1),
            new ExcelSmtLineAssignmentImportRow("Base FMD SMT", "M1", "BRONCO", "SMT", "SMT 2", 2, 1m, "Agosto 2026", 1)
        ],
        SmtCapacityTargets: [],
        SourceWarnings: []);
}
