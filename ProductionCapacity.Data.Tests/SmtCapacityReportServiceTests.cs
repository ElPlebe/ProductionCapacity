using Microsoft.EntityFrameworkCore;
using ProductionCapacity.Data;
using ProductionCapacity.Data.Entities;

namespace ProductionCapacity.Data.Tests;

public sealed class SmtCapacityReportServiceTests
{
    [Fact]
    public async Task ReportCalculatesSmtCapacityByFamilyLineAndWeek()
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
            WorkDays = 5.66m,
            HoursPerShift = 7.5m,
            ShiftsPerDay = 3,
            EquipmentQuantity = 1m
        });
        db.PartNumbers.Add(new PartNumberEntity
        {
            PartNumberId = 1,
            PartNumber = "68-7103",
            Description = "Azteca",
            ProductFamily = "AZTECA",
            DefaultYield = 1m
        });
        db.WeeklyDemand.Add(new WeeklyDemandEntity
        {
            ScenarioId = 1,
            ProductionWeekId = 1,
            PartNumberId = 1,
            DemandUnits = 28.25m
        });
        db.SmtFamilyVolumes.Add(new SmtFamilyVolumeEntity
        {
            ScenarioId = 1,
            ProductionWeekId = 1,
            ProductFamily = "AZTECA",
            BusinessUnit = "ERBU",
            MonthName = "AUG",
            MonthSequence = 1,
            MonthlyMps = 28.25m,
            WeeklyMps = 7.0625m
        });
        db.SmtFamilyRates.Add(new SmtFamilyRateEntity
        {
            OperationId = 1,
            ProductFamily = "AZTECA",
            UnitsPerHour = 2.176590656m,
            ChangeoverCount = 4m,
            BottleneckUnitsPerHour = 2.176590656m
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
            MonthName = "AUG",
            MonthSequence = 1
        });
        await db.SaveChangesAsync();
        var service = new SmtCapacityReportService(db);

        var report = await service.GetReportAsync("Base semanal");

        var row = Assert.Single(report.Rows);
        Assert.Equal("WK13", row.WeekName);
        Assert.Equal("SMT 1", row.LineCode);
        Assert.Equal("AZTECA", row.ProductFamily);
        Assert.Equal(7.063m, row.WeeklyDemandUnits);
        Assert.Equal(28.25m, row.AssignedDemandUnits);
        Assert.Equal(16m, row.ChangeoverHoursMonth);
        Assert.Equal(493.400m, row.AvailableHours);
        Assert.Equal(912.841m, row.MaxCapacityUnits);
        Assert.Equal(2.631m, row.UtilizationPercent);
        Assert.Equal(884.591m, row.DeltaCapacityUnits);
    }

    [Fact]
    public async Task UpdateLineAssignmentMovesFamilyAndUpdatesDistributionFactor()
    {
        await using var db = CreateContext();
        db.Scenarios.Add(new Scenario { ScenarioId = 1, ScenarioName = "Base semanal" });
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
            OperationName = "Surface Mount",
            OperationFamily = "SMT",
            SequenceNumber = 1
        });
        db.SmtLineAssignments.Add(new SmtLineAssignmentEntity
        {
            SmtLineAssignmentId = 1,
            ScenarioId = 1,
            ProductionWeekId = 1,
            OperationId = 1,
            ProductFamily = "AZTECA",
            LineCode = "SMT 1",
            LineNumber = 1,
            LineDistribution = 1m,
            MonthName = "Agosto 2026",
            MonthSequence = 1,
            IsActive = true
        });
        await db.SaveChangesAsync();
        var service = new SmtCapacityReportService(db);

        await service.UpdateLineAssignmentAsync(new SmtCapacityAssignmentEdit(
            "Base semanal",
            "M1",
            "SMT",
            "AZTECA",
            "SMT 1",
            "SMT 3",
            65m));

        var assignment = Assert.Single(await db.SmtLineAssignments.ToListAsync());
        Assert.Equal("SMT 3", assignment.LineCode);
        Assert.Equal(3, assignment.LineNumber);
        Assert.Equal(0.65m, assignment.LineDistribution);
    }


    [Fact]
    public async Task ReportBuildsExcelStyleWeekColumnsAndLineBlocks()
    {
        await using var db = CreateContext();
        db.Scenarios.Add(new Scenario { ScenarioId = 1, ScenarioName = "Base semanal" });
        db.ProductionWeeks.AddRange(
            new ProductionWeekEntity
            {
                ProductionWeekId = 1,
                ScenarioId = 1,
                WeekName = "WK13",
                WorkDays = 5m,
                HoursPerShift = 7.5m,
                ShiftsPerDay = 3
            },
            new ProductionWeekEntity
            {
                ProductionWeekId = 2,
                ScenarioId = 1,
                WeekName = "WK14",
                WorkDays = 5m,
                HoursPerShift = 7.5m,
                ShiftsPerDay = 3
            });
        db.Operations.Add(new PlanningOperation
        {
            OperationId = 1,
            OperationCode = "SMT",
            OperationName = "Surface Mount",
            OperationFamily = "SMT",
            SequenceNumber = 1
        });
        db.OperationWeeklyCapacities.AddRange(
            new OperationWeeklyCapacityEntity
            {
                ScenarioId = 1,
                ProductionWeekId = 1,
                OperationId = 1,
                WorkDays = 5m,
                HoursPerShift = 7.5m,
                ShiftsPerDay = 3,
                EquipmentQuantity = 1m
            },
            new OperationWeeklyCapacityEntity
            {
                ScenarioId = 1,
                ProductionWeekId = 2,
                OperationId = 1,
                WorkDays = 5m,
                HoursPerShift = 7.5m,
                ShiftsPerDay = 3,
                EquipmentQuantity = 1m
            });
        db.SmtFamilyVolumes.AddRange(
            new SmtFamilyVolumeEntity
            {
                ScenarioId = 1,
                ProductionWeekId = 1,
                ProductFamily = "AZTECA",
                BusinessUnit = "ERBU",
                MonthName = "AUG",
                MonthSequence = 1,
                MonthlyMps = 100m,
                WeeklyMps = 25m
            },
            new SmtFamilyVolumeEntity
            {
                ScenarioId = 1,
                ProductionWeekId = 2,
                ProductFamily = "AZTECA",
                BusinessUnit = "ERBU",
                MonthName = "AUG",
                MonthSequence = 1,
                MonthlyMps = 100m,
                WeeklyMps = 25m
            });
        db.SmtFamilyRates.Add(new SmtFamilyRateEntity
        {
            OperationId = 1,
            ProductFamily = "AZTECA",
            UnitsPerHour = 1m,
            ChangeoverCount = 1m,
            BottleneckUnitsPerHour = 1m
        });
        db.SmtLineAssignments.AddRange(
            new SmtLineAssignmentEntity
            {
                ScenarioId = 1,
                ProductionWeekId = 1,
                OperationId = 1,
                ProductFamily = "AZTECA",
                LineCode = "SMT 1",
                LineNumber = 1,
                LineDistribution = 1m,
                MonthName = "AUG",
                MonthSequence = 1
            },
            new SmtLineAssignmentEntity
            {
                ScenarioId = 1,
                ProductionWeekId = 2,
                OperationId = 1,
                ProductFamily = "AZTECA",
                LineCode = "SMT 1",
                LineNumber = 1,
                LineDistribution = 1m,
                MonthName = "AUG",
                MonthSequence = 1
            });
        await db.SaveChangesAsync();
        var service = new SmtCapacityReportService(db);

        var report = await service.GetReportAsync("Base semanal");

        Assert.Collection(
            report.WeekColumns,
            week =>
            {
                Assert.Equal("WK13", week.WeekName);
                Assert.Equal("AUG", week.MonthName);
            },
            week =>
            {
                Assert.Equal("WK14", week.WeekName);
                Assert.Equal("AUG", week.MonthName);
            });
        var block = Assert.Single(report.LineBlocks);
        Assert.Equal("SMT 1", block.LineCode);
        var family = Assert.Single(block.FamilyRows);
        Assert.Equal("AZTECA", family.ProductFamily);
        Assert.Equal("SMT", family.OperationCode);
        Assert.Equal("SMT 1", family.LineCode);
        Assert.Equal(1, family.LineNumber);
        Assert.Equal(1m, family.LineDistribution);
        Assert.True(family.WeekCells.ContainsKey("WK13"));
        Assert.True(family.WeekCells.ContainsKey("WK14"));
        Assert.Equal(100m, family.WeekCells["WK13"].AssignedDemandUnits);
        Assert.Equal(100m, family.WeekCells["WK14"].AssignedDemandUnits);
        Assert.True(block.WeeklySummaries.ContainsKey("WK13"));
        Assert.True(block.WeeklySummaries.ContainsKey("WK14"));
    }

    [Fact]
    public async Task ReportUsesLineSpecificUphWhenAvailable()
    {
        await using var db = CreateContext();
        db.Scenarios.Add(new Scenario { ScenarioId = 1, ScenarioName = "Base semanal" });
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
            MonthName = "M1",
            MonthSequence = 1,
            MonthlyMps = 100m,
            WeeklyMps = 25m
        });
        db.SmtFamilyRates.Add(new SmtFamilyRateEntity
        {
            OperationId = 1,
            ProductFamily = "AZTECA",
            UnitsPerHour = 10m,
            ChangeoverCount = 0m
        });
        db.SmtLineFamilyRates.Add(new SmtLineFamilyRateEntity
        {
            OperationId = 1,
            ProductFamily = "AZTECA",
            LineCode = "SMT 1",
            LineNumber = 1,
            UnitsPerHour = 20m
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
        await db.SaveChangesAsync();
        var service = new SmtCapacityReportService(db);

        var report = await service.GetReportAsync("Base semanal");

        var row = Assert.Single(report.Rows);
        Assert.Equal(20m, row.UnitsPerHour);
        Assert.Equal(2.5m, row.UtilizationPercent);
    }

    [Fact]
    public async Task ReportSubtractsChangeoverHoursFromAvailableCapacity()
    {
        await using var db = CreateContext();
        db.Scenarios.Add(new Scenario { ScenarioId = 1, ScenarioName = "Base semanal" });
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
            MonthName = "M1",
            MonthSequence = 1,
            MonthlyMps = 100m,
            WeeklyMps = 25m
        });
        db.SmtFamilyRates.Add(new SmtFamilyRateEntity
        {
            OperationId = 1,
            ProductFamily = "AZTECA",
            UnitsPerHour = 10m,
            ChangeoverCount = 3m
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
        await db.SaveChangesAsync();
        var service = new SmtCapacityReportService(db);

        var report = await service.GetReportAsync("Base semanal");

        var row = Assert.Single(report.Rows);
        Assert.Equal(12m, row.ChangeoverHoursMonth);
        Assert.Equal(188m, row.AvailableHours);
        Assert.Equal(5.320m, row.UtilizationPercent);
        Assert.Equal(1598m, row.MaxCapacityUnits);
        Assert.Equal(1498m, row.DeltaCapacityUnits);
    }

    private static PlanningDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PlanningDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new PlanningDbContext(options);
    }
}
