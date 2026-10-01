using Microsoft.EntityFrameworkCore;
using ProductionCapacity.Data;
using ProductionCapacity.Data.Entities;
using ProductionCapacity.Data.ExcelImport;

namespace ProductionCapacity.Data.Tests;

public sealed class ParityDataServicesTests
{
    [Fact]
    public async Task SavesPartCrossReferenceAndRejectsDuplicateReferenceValue()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var db = CreateContext(databaseName);
        db.PartNumbers.Add(new PartNumberEntity
        {
            PartNumberId = 1,
            PartNumber = "68-103450",
            Description = "Longball",
            DefaultYield = 1m
        });
        await db.SaveChangesAsync();
        var service = new CatalogAdministrationService(new TestDbContextFactory(databaseName));

        await service.SavePartCrossReferenceAsync(new PartCrossReferenceCatalogItem
        {
            PartNumberId = 1,
            ReferenceType = "73_LEVEL",
            ReferenceValue = "73-103450"
        });

        var duplicate = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SavePartCrossReferenceAsync(new PartCrossReferenceCatalogItem
            {
                PartNumberId = 1,
                ReferenceType = "73_LEVEL",
                ReferenceValue = "73-103450"
            }));
        Assert.Contains("referencia ya existe", duplicate.Message);
    }

    [Fact]
    public async Task PlanningMatrixCanReturnAllWeeksInScenario()
    {
        await using var db = CreateContext();
        await SeedTwoWeekMatrixAsync(db);
        var service = new PlanningMatrixService(db);

        var matrices = await service.GetMatricesAsync("Base semanal");

        Assert.Collection(
            matrices,
            first => Assert.Equal("WK13", first.WeekName),
            second => Assert.Equal("WK14", second.WeekName));
        Assert.Equal(127.35m, matrices[0].OperationSummaries["ASSY"].AvailableHours);
        Assert.Equal(104.85m, matrices[1].OperationSummaries["ASSY"].AvailableHours);
    }

    [Fact]
    public async Task ExcelImportPreviewWarnsAboutBrokenReferencesAndApplyCreatesRates()
    {
        await using var db = CreateContext();
        db.Scenarios.Add(new Scenario { ScenarioId = 1, ScenarioName = "Base semanal" });
        db.ProductionWeeks.Add(new ProductionWeekEntity
        {
            ProductionWeekId = 1,
            ScenarioId = 1,
            WeekName = "WK13",
            WorkDays = 5.66m,
            HoursPerShift = 7.5m,
            ShiftsPerDay = 3
        });
        db.PartNumbers.Add(new PartNumberEntity
        {
            PartNumberId = 1,
            PartNumber = "68-103450",
            Description = "Longball",
            DefaultYield = 1m
        });
        await db.SaveChangesAsync();
        var service = new ExcelCapacityImportService(db);
        var package = new ExcelCapacityImportPackage(
            Scenarios: [],
            ProductionWeeks: [],
            PartNumbers: [],
            WeeklyDemands: [],
            Operations:
            [
                new ExcelOperationImportRow("ASSY", "ASSY", "Assembly", 1)
            ],
            OperationRates:
            [
                new ExcelOperationRateImportRow("68-103450", null, "ASSY", 73.8684m, 1m, 1m, 1m, null, null)
            ],
            OperationCapacities:
            [
                new ExcelOperationCapacityImportRow("Base semanal", "WK13", "ASSY", 5.66m, 7.5m, 3, 1m, 0m, 0m, 0m)
            ],
            CrossReferences:
            [
                new ExcelCrossReferenceImportRow("68-103450", "73_LEVEL", "73-103450")
            ],
            TestTimes: [],
            ResourceTypes: [],
            ResourceConfigurations: [],
            ResourceInventories: [],
            SmtFamilyVolumes: [],
            SmtFamilyRates: [],
            SmtLineFamilyRates: [],
            SmtLineAssignments: [],
            SmtCapacityTargets: [],
            SourceWarnings:
            [
                "PLAN UABU contiene referencia #REF! en una formula revisada."
            ]);

        var preview = await service.PreviewAsync(package);
        await service.ApplyAsync(package);

        Assert.Contains(preview.Warnings, warning => warning.Contains("#REF!"));
        Assert.Single(db.Operations);
        Assert.Single(db.OperationRates);
        Assert.Single(db.PartCrossReferences);
    }

    [Fact]
    public async Task ExcelImportAppliesSmtFamilyRowsToFamilyPartsAndStoresSmtInputs()
    {
        await using var db = CreateContext();
        db.Scenarios.Add(new Scenario { ScenarioId = 1, ScenarioName = "Base semanal" });
        db.ProductionWeeks.Add(new ProductionWeekEntity
        {
            ProductionWeekId = 1,
            ScenarioId = 1,
            WeekName = "WK13",
            WorkDays = 5.66m,
            HoursPerShift = 7.5m,
            ShiftsPerDay = 3
        });
        db.PartNumbers.AddRange(
            new PartNumberEntity
            {
                PartNumberId = 1,
                PartNumber = "68-7103",
                Description = "Azteca A",
                ProductFamily = "AZTECA",
                DefaultYield = 1m
            },
            new PartNumberEntity
            {
                PartNumberId = 2,
                PartNumber = "68-7225",
                Description = "Azteca B",
                ProductFamily = "AZTECA",
                DefaultYield = 1m
            });
        await db.SaveChangesAsync();
        var service = new ExcelCapacityImportService(db);
        var package = new ExcelCapacityImportPackage(
            Scenarios: [],
            ProductionWeeks: [],
            PartNumbers: [],
            WeeklyDemands:
            [
                new ExcelWeeklyDemandImportRow("Base semanal", "WK13", null, "AZTECA", 28.25m)
            ],
            Operations:
            [
                new ExcelOperationImportRow("SMT", "Surface Mount", "SMT", 1)
            ],
            OperationRates: [],
            OperationCapacities: [],
            CrossReferences: [],
            TestTimes: [],
            ResourceTypes: [],
            ResourceConfigurations: [],
            ResourceInventories: [],
            SmtFamilyVolumes:
            [
                new ExcelSmtFamilyVolumeImportRow("Base semanal", "WK13", "ERBU", "AZTECA", "AUG", 1, 113m, 28.25m)
            ],
            SmtFamilyRates:
            [
                new ExcelSmtFamilyRateImportRow("AZTECA", "SMT", 2.176590656m, 4m, null, 2.176590656m, 38.084415584m, 17m, 3.636363636m)
            ],
            SmtLineFamilyRates: [],
            SmtLineAssignments:
            [
                new ExcelSmtLineAssignmentImportRow("Base semanal", "WK13", "AZTECA", "SMT", "SMT 1", 1, 1m, "AUG", 1)
            ],
            SmtCapacityTargets: [],
            SourceWarnings: []);

        await service.ApplyAsync(package);

        var demands = await db.WeeklyDemand.OrderBy(item => item.PartNumberId).ToListAsync();
        Assert.Collection(
            demands,
            first => Assert.Equal(14.125m, first.DemandUnits),
            second => Assert.Equal(14.125m, second.DemandUnits));
        var rates = await db.OperationRates.OrderBy(item => item.PartNumberId).ToListAsync();
        Assert.All(rates, rate => Assert.Equal(Math.Round(3600m / 2.176590656m, 6), rate.StandardTimeSeconds));
        Assert.Single(db.SmtFamilyVolumes);
        Assert.Single(db.SmtFamilyRates);
        Assert.Single(db.SmtLineAssignments);
    }

    private static PlanningDbContext CreateContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<PlanningDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;

        return new PlanningDbContext(options);
    }

    private static async Task SeedTwoWeekMatrixAsync(PlanningDbContext db)
    {
        db.Scenarios.Add(new Scenario { ScenarioId = 1, ScenarioName = "Base semanal" });
        db.ProductionWeeks.AddRange(
            new ProductionWeekEntity
            {
                ProductionWeekId = 1,
                ScenarioId = 1,
                WeekName = "WK13",
                WorkDays = 5.66m,
                HoursPerShift = 7.5m,
                ShiftsPerDay = 3
            },
            new ProductionWeekEntity
            {
                ProductionWeekId = 2,
                ScenarioId = 1,
                WeekName = "WK14",
                WorkDays = 5.66m,
                HoursPerShift = 7.5m,
                ShiftsPerDay = 3,
                HolidayDays = 1m
            });
        db.PartNumbers.Add(new PartNumberEntity
        {
            PartNumberId = 1,
            PartNumber = "68-103450",
            Description = "Longball",
            DefaultYield = 1m
        });
        db.WeeklyDemand.AddRange(
            new WeeklyDemandEntity
            {
                WeeklyDemandId = 1,
                ScenarioId = 1,
                ProductionWeekId = 1,
                PartNumberId = 1,
                DemandUnits = 1_000m
            },
            new WeeklyDemandEntity
            {
                WeeklyDemandId = 2,
                ScenarioId = 1,
                ProductionWeekId = 2,
                PartNumberId = 1,
                DemandUnits = 1_000m
            });
        db.Operations.Add(new PlanningOperation
        {
            OperationId = 1,
            OperationCode = "ASSY",
            OperationName = "ASSY",
            SequenceNumber = 1
        });
        db.OperationWeeklyCapacities.AddRange(
            new OperationWeeklyCapacityEntity
            {
                OperationWeeklyCapacityId = 1,
                ScenarioId = 1,
                ProductionWeekId = 1,
                OperationId = 1,
                WorkDays = 5.66m,
                HoursPerShift = 7.5m,
                ShiftsPerDay = 3,
                EquipmentQuantity = 1m
            },
            new OperationWeeklyCapacityEntity
            {
                OperationWeeklyCapacityId = 2,
                ScenarioId = 1,
                ProductionWeekId = 2,
                OperationId = 1,
                WorkDays = 5.66m,
                HoursPerShift = 7.5m,
                ShiftsPerDay = 3,
                EquipmentQuantity = 1m,
                HolidayDays = 1m
            });
        db.OperationRates.Add(new OperationRateEntity
        {
            OperationRateId = 1,
            PartNumberId = 1,
            OperationId = 1,
            StandardTimeSeconds = 73.8684m,
            YieldFactor = 1m,
            UtilizationFactor = 1m,
            EfficiencyFactor = 1m
        });

        await db.SaveChangesAsync();
    }

    private sealed class TestDbContextFactory(string databaseName) : IDbContextFactory<PlanningDbContext>
    {
        public PlanningDbContext CreateDbContext() => CreateContext(databaseName);

        public Task<PlanningDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateContext(databaseName));
    }
}
