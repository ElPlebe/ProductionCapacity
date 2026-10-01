using Microsoft.EntityFrameworkCore;
using ProductionCapacity.Data;
using ProductionCapacity.Data.Entities;

namespace ProductionCapacity.Data.Tests;

public sealed class SmtDiagnosticsServiceTests
{
    [Fact]
    public async Task SnapshotFlagsMissingReadinessDataForSmtPlanning()
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
        db.SmtFamilyVolumes.Add(new SmtFamilyVolumeEntity
        {
            ScenarioId = 1,
            ProductionWeekId = 1,
            ProductFamily = "AZTECA",
            BusinessUnit = "ERBU",
            MonthName = "M1",
            MonthSequence = 1,
            MonthlyMps = 100m,
            WeeklyMps = 25m
        });
        db.SmtLineAssignments.Add(new SmtLineAssignmentEntity
        {
            ScenarioId = 1,
            ProductionWeekId = 1,
            OperationId = 1,
            ProductFamily = "AZTECA",
            LineCode = "SMT 1",
            LineNumber = 1,
            LineDistribution = 0.8m,
            MonthName = "M1",
            MonthSequence = 1
        });
        await db.SaveChangesAsync();
        var service = new SmtDiagnosticsService(db);

        var snapshot = await service.GetSnapshotAsync("Base FMD SMT");

        Assert.Equal("Base FMD SMT", snapshot.ScenarioName);
        Assert.Contains(snapshot.Issues, issue =>
            issue.Severity == SmtDiagnosticSeverity.Critical &&
            issue.Category == "UPH" &&
            issue.EntityKey == "AZTECA");
        Assert.Contains(snapshot.Issues, issue =>
            issue.Severity == SmtDiagnosticSeverity.Critical &&
            issue.Category == "Distribucion" &&
            issue.EntityKey == "AZTECA|M1");
        Assert.Contains(snapshot.Issues, issue =>
            issue.Severity == SmtDiagnosticSeverity.Critical &&
            issue.Category == "Capacidad" &&
            issue.EntityKey == "SMT|M1");
        Assert.Contains(snapshot.Issues, issue =>
            issue.Severity == SmtDiagnosticSeverity.Warning &&
            issue.Category == "Targets Excel" &&
            issue.EntityKey == "AZTECA|M1|SMT 1");
        Assert.Contains(snapshot.Cards, card => card.Title == "UPH por familia" && card.Status == SmtDiagnosticStatus.Critical);
        Assert.True(snapshot.CriticalCount >= 3);
    }

    private static PlanningDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PlanningDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new PlanningDbContext(options);
    }
}
