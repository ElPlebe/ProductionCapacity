using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ProductionCapacity.Data;
using ProductionCapacity.Data.Entities;

namespace ProductionCapacity.Data.Tests;

public sealed class SmtAdministrationServiceTests
{
    [Fact]
    public async Task SavesSmtFamilyAndListsItWithRelatedPartCount()
    {
        var factory = CreateFactory();
        await SeedOperationAsync(factory);
        var service = new SmtAdministrationService(factory);

        var familyId = await service.SaveFamilyAsync(new SmtFamilyCatalogItem
        {
            ProductFamily = "AZTECA",
            BusinessUnit = "ERBU",
            Notes = "Familia base SMT",
            IsActive = true
        });

        await using (var db = await factory.CreateDbContextAsync())
        {
            db.PartNumbers.Add(new PartNumberEntity
            {
                PartNumber = "68-1000",
                Description = "Producto AZTECA",
                BusinessUnit = "ERBU",
                ProductFamily = "AZTECA",
                DefaultYield = 1m,
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        var snapshot = await service.GetSnapshotAsync();

        var family = Assert.Single(snapshot.Families);
        Assert.Equal(familyId, family.SmtFamilyId);
        Assert.Equal("AZTECA", family.ProductFamily);
        Assert.Equal("ERBU", family.BusinessUnit);
        Assert.Equal(1, family.PartNumberCount);
    }

    [Fact]
    public async Task SavesFamilyRateAndLineFamilyRateWithLookupNames()
    {
        var factory = CreateFactory();
        await SeedOperationAsync(factory);
        var service = new SmtAdministrationService(factory);
        await service.SaveFamilyAsync(new SmtFamilyCatalogItem
        {
            ProductFamily = "AZTECA",
            BusinessUnit = "ERBU",
            IsActive = true
        });

        var rateId = await service.SaveFamilyRateAsync(new SmtFamilyRateCatalogItem
        {
            ProductFamily = "AZTECA",
            OperationId = 1,
            UnitsPerHour = 2.18m,
            ChangeoverCount = 4m,
            SourcePartNumber = "68-1000",
            BottleneckUnitsPerHour = 2.18m,
            RouterUnitsPerHour = 38m,
            WaveUnitsPerHour = 17m,
            PressfitUnitsPerHour = 3.6m,
            IsActive = true
        });
        var lineRateId = await service.SaveLineFamilyRateAsync(new SmtLineFamilyRateCatalogItem
        {
            ProductFamily = "AZTECA",
            OperationId = 1,
            LineCode = "SMT 1",
            LineNumber = 1,
            UnitsPerHour = 2.50m,
            SourcePartNumber = "68-1000",
            BottleneckUnitsPerHour = 2.50m,
            RouterUnitsPerHour = 39m,
            WaveUnitsPerHour = 18m,
            PressfitUnitsPerHour = 4m,
            IsActive = true
        });

        var snapshot = await service.GetSnapshotAsync();

        var rate = Assert.Single(snapshot.FamilyRates);
        Assert.Equal(rateId, rate.SmtFamilyRateId);
        Assert.Equal("SMT", rate.OperationCode);
        Assert.Equal(2.18m, rate.UnitsPerHour);

        var lineRate = Assert.Single(snapshot.LineFamilyRates);
        Assert.Equal(lineRateId, lineRate.SmtLineFamilyRateId);
        Assert.Equal("SMT 1", lineRate.LineCode);
        Assert.Equal(2.50m, lineRate.UnitsPerHour);
    }

    [Fact]
    public async Task RejectsDuplicateFamiliesIgnoringCase()
    {
        var factory = CreateFactory();
        var service = new SmtAdministrationService(factory);
        await service.SaveFamilyAsync(new SmtFamilyCatalogItem
        {
            ProductFamily = "AZTECA",
            BusinessUnit = "ERBU",
            IsActive = true
        });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveFamilyAsync(new SmtFamilyCatalogItem
            {
                ProductFamily = "azteca",
                BusinessUnit = "ERBU",
                IsActive = true
            }));

        Assert.Contains("familia SMT ya existe", error.Message);
    }

    [Fact]
    public async Task SavesAuditTrailForSmtAdministrationChanges()
    {
        var factory = CreateFactory();
        await SeedOperationAsync(factory);
        var service = new SmtAdministrationService(factory);

        await service.SaveFamilyAsync(new SmtFamilyCatalogItem
        {
            ProductFamily = "AZTECA",
            BusinessUnit = "ERBU",
            IsActive = true
        });
        await service.SaveFamilyRateAsync(new SmtFamilyRateCatalogItem
        {
            ProductFamily = "AZTECA",
            OperationId = 1,
            UnitsPerHour = 2.18m,
            ChangeoverCount = 4m,
            IsActive = true
        });

        var snapshot = await service.GetSnapshotAsync();

        Assert.Contains(snapshot.AuditLogs, log =>
            log.EntityType == "Familia SMT" &&
            log.EntityKey == "AZTECA" &&
            log.ActionType == "Creacion" &&
            log.ChangedBy == "Admin SMT");
        Assert.Contains(snapshot.AuditLogs, log =>
            log.EntityType == "UPH Familia SMT" &&
            log.EntityKey == "AZTECA|SMT" &&
            log.NewValue!.Contains("UPH 2.18", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SavesLineDistributionAndSmtCalendarCapacity()
    {
        var factory = CreateFactory();
        await SeedScenarioWeekOperationAsync(factory);
        var service = new SmtAdministrationService(factory);
        await service.SaveFamilyAsync(new SmtFamilyCatalogItem
        {
            ProductFamily = "AZTECA",
            BusinessUnit = "ERBU",
            IsActive = true
        });

        var assignmentId = await service.SaveLineAssignmentAsync(new SmtLineAssignmentCatalogItem
        {
            ScenarioId = 1,
            ProductionWeekId = 1,
            OperationId = 1,
            ProductFamily = "AZTECA",
            LineCode = "SMT 1",
            LineNumber = 1,
            LineDistributionPercent = 60m,
            MonthName = "M1",
            MonthSequence = 1,
            IsActive = true
        });
        var capacityId = await service.SaveOperationCapacityAsync(new SmtOperationCapacityCatalogItem
        {
            ScenarioId = 1,
            ProductionWeekId = 1,
            OperationId = 1,
            WorkDays = 5.66m,
            HoursPerShift = 7.5m,
            ShiftsPerDay = 3,
            EquipmentQuantity = 6m,
            HolidayDays = 1m,
            OvertimeHours = 2m,
            PlannedDowntimeHours = 3m
        });

        var snapshot = await service.GetSnapshotAsync();

        var assignment = Assert.Single(snapshot.LineAssignments);
        Assert.Equal(assignmentId, assignment.SmtLineAssignmentId);
        Assert.Equal("Base FMD SMT", assignment.ScenarioName);
        Assert.Equal("M1", assignment.WeekName);
        Assert.Equal("SMT", assignment.OperationCode);
        Assert.Equal("AZTECA", assignment.ProductFamily);
        Assert.Equal(60m, assignment.LineDistributionPercent);

        var capacity = Assert.Single(snapshot.OperationCapacities);
        Assert.Equal(capacityId, capacity.OperationWeeklyCapacityId);
        Assert.Equal("Base FMD SMT", capacity.ScenarioName);
        Assert.Equal("M1", capacity.WeekName);
        Assert.Equal("SMT", capacity.OperationCode);
        Assert.Equal(5.66m, capacity.WorkDays);
        Assert.Equal(1m, capacity.HolidayDays);
        Assert.Equal(2m, capacity.OvertimeHours);
        Assert.Equal(3m, capacity.PlannedDowntimeHours);
    }

    [Fact]
    public async Task SavesCapacityTargetsAndRejectsInvalidSmtCatalogInputs()
    {
        var factory = CreateFactory();
        await SeedScenarioWeekOperationAsync(factory);
        var service = new SmtAdministrationService(factory);

        var targetId = await service.SaveCapacityTargetAsync(new SmtCapacityTargetCatalogItem
        {
            ScenarioId = 1,
            ProductionWeekId = 1,
            OperationId = 1,
            ProductFamily = "AZTECA",
            LineCode = "SMT 1",
            ExpectedAssignedDemandUnits = 100m,
            ExpectedMaxCapacityUnits = 250m,
            ExpectedUtilizationPercent = 40m,
            ExpectedDeltaCapacityUnits = 150m,
            SourceReference = "Capacity SMT!B5:Q5",
            Notes = "Target base"
        });

        var invalidDistribution = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveLineAssignmentAsync(new SmtLineAssignmentCatalogItem
            {
                ScenarioId = 1,
                ProductionWeekId = 1,
                OperationId = 1,
                ProductFamily = "AZTECA",
                LineCode = "SMT 1",
                LineNumber = 1,
                LineDistributionPercent = 125m,
                IsActive = true
            }));
        var invalidCalendar = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveOperationCapacityAsync(new SmtOperationCapacityCatalogItem
            {
                ScenarioId = 1,
                ProductionWeekId = 1,
                OperationId = 1,
                WorkDays = 5m,
                HoursPerShift = 0m,
                ShiftsPerDay = 3,
                EquipmentQuantity = 1m
            }));

        var snapshot = await service.GetSnapshotAsync();

        var target = Assert.Single(snapshot.CapacityTargets);
        Assert.Equal(targetId, target.SmtCapacityTargetId);
        Assert.Equal("Base FMD SMT", target.ScenarioName);
        Assert.Equal("M1", target.WeekName);
        Assert.Equal("SMT", target.OperationCode);
        Assert.Equal("AZTECA", target.ProductFamily);
        Assert.Equal("SMT 1", target.LineCode);
        Assert.Equal(40m, target.ExpectedUtilizationPercent);
        Assert.Contains("Distribucion", invalidDistribution.Message);
        Assert.Contains("Horas por turno", invalidCalendar.Message);
    }

    private static IDbContextFactory<PlanningDbContext> CreateFactory()
    {
        var options = new DbContextOptionsBuilder<PlanningDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new PooledDbContextFactory<PlanningDbContext>(options);
    }

    private static async Task SeedOperationAsync(IDbContextFactory<PlanningDbContext> factory)
    {
        await using var db = await factory.CreateDbContextAsync();
        db.Operations.Add(new PlanningOperation
        {
            OperationId = 1,
            OperationCode = "SMT",
            OperationName = "SMT",
            OperationFamily = "SMT",
            SequenceNumber = 1,
            IsActive = true
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedScenarioWeekOperationAsync(IDbContextFactory<PlanningDbContext> factory)
    {
        await using var db = await factory.CreateDbContextAsync();
        db.Scenarios.Add(new Scenario
        {
            ScenarioId = 1,
            ScenarioName = "Base FMD SMT",
            IsBaseline = true
        });
        db.ProductionWeeks.Add(new ProductionWeekEntity
        {
            ProductionWeekId = 1,
            ScenarioId = 1,
            WeekName = "M1",
            WorkDays = 5.66m,
            HoursPerShift = 7.5m,
            ShiftsPerDay = 3
        });
        db.Operations.Add(new PlanningOperation
        {
            OperationId = 1,
            OperationCode = "SMT",
            OperationName = "SMT",
            OperationFamily = "SMT",
            SequenceNumber = 1,
            IsActive = true
        });
        await db.SaveChangesAsync();
    }
}
