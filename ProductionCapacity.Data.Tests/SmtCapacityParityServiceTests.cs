using Microsoft.EntityFrameworkCore;
using ProductionCapacity.Data;
using ProductionCapacity.Data.Entities;

namespace ProductionCapacity.Data.Tests;

public sealed class SmtCapacityParityServiceTests
{
    [Fact]
    public async Task CompareTargetsMatchesExcelCapacitySmtRows()
    {
        await using var db = CreateContext();
        db.Scenarios.Add(new Scenario { ScenarioId = 1, ScenarioName = "Base FMD SMT" });
        db.ProductionWeeks.Add(new ProductionWeekEntity
        {
            ProductionWeekId = 1,
            ScenarioId = 1,
            WeekName = "M1",
            WorkDays = 5m,
            HoursPerShift = 10m,
            ShiftsPerDay = 1
        });
        db.Operations.Add(new PlanningOperation
        {
            OperationId = 1,
            OperationCode = "SMT",
            OperationName = "Surface Mount",
            OperationFamily = "SMT",
            SequenceNumber = 1
        });
        db.OperationWeeklyCapacities.Add(new OperationWeeklyCapacityEntity
        {
            ScenarioId = 1,
            ProductionWeekId = 1,
            OperationId = 1,
            WorkDays = 5m,
            HoursPerShift = 10m,
            ShiftsPerDay = 1,
            EquipmentQuantity = 1m
        });
        db.SmtFamilyVolumes.Add(new SmtFamilyVolumeEntity
        {
            ScenarioId = 1,
            ProductionWeekId = 1,
            ProductFamily = "AZTECA",
            BusinessUnit = "ERBU",
            MonthName = "M1",
            MonthSequence = 1,
            MonthlyMps = 50m,
            WeeklyMps = 12.5m
        });
        db.SmtFamilyRates.Add(new SmtFamilyRateEntity
        {
            OperationId = 1,
            ProductFamily = "AZTECA",
            UnitsPerHour = 10m,
            ChangeoverCount = 0m
        });
        db.SmtLineAssignments.Add(new SmtLineAssignmentEntity
        {
            ScenarioId = 1,
            ProductionWeekId = 1,
            OperationId = 1,
            ProductFamily = "AZTECA",
            LineCode = "SMT 1",
            LineNumber = 1,
            LineDistribution = 1m,
            MonthName = "M1",
            MonthSequence = 1
        });
        db.SmtCapacityTargets.Add(new SmtCapacityTargetEntity
        {
            ScenarioId = 1,
            ProductionWeekId = 1,
            OperationId = 1,
            ProductFamily = "AZTECA",
            LineCode = "SMT 1",
            ExpectedAssignedDemandUnits = 50m,
            ExpectedMaxCapacityUnits = 1700m,
            ExpectedUtilizationPercent = 2.5m,
            ExpectedDeltaCapacityUnits = 1650m,
            SourceReference = "Capacity SMT!D5:N5"
        });
        await db.SaveChangesAsync();
        var service = new SmtCapacityParityService(db, new SmtCapacityReportService(db));

        var result = await service.CompareAsync("Base FMD SMT", tolerance: 0.001m);

        Assert.True(result.IsPassing);
        var row = Assert.Single(result.Rows);
        Assert.Equal("M1", row.WeekName);
        Assert.Equal("SMT 1", row.LineCode);
        Assert.Equal("AZTECA", row.ProductFamily);
        Assert.Equal(1700m, row.ActualMaxCapacityUnits);
        Assert.Equal(2.5m, row.ActualUtilizationPercent);
        Assert.Null(row.Message);
    }

    private static PlanningDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PlanningDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new PlanningDbContext(options);
    }
}
