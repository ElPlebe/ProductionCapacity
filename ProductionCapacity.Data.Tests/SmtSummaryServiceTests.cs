using Microsoft.EntityFrameworkCore;
using ProductionCapacity.Data.Entities;

namespace ProductionCapacity.Data.Tests;

public sealed class SmtSummaryServiceTests
{
    [Fact]
    public async Task SummaryBuildsLinearityProjectionAndCapacityBySmtMonth()
    {
        await using var db = CreateContext();
        db.Scenarios.Add(new Scenario { ScenarioId = 1, ScenarioName = "Base FMD SMT" });
        db.ProductionWeeks.AddRange(
            Week(1, "M1"),
            Week(2, "M2"),
            Week(3, "M3"));
        db.Operations.Add(new PlanningOperation
        {
            OperationId = 1,
            OperationCode = "SMT",
            OperationName = "Surface Mount",
            OperationFamily = "SMT",
            SequenceNumber = 1
        });
        db.OperationWeeklyCapacities.AddRange(
            Capacity(1),
            Capacity(2),
            Capacity(3));
        db.SmtFamilyVolumes.AddRange(
            Volume(1, "AZTECA", "ERBU", "Agosto 2026", 1, 25m),
            Volume(2, "AZTECA", "ERBU", "Septiembre 2026", 2, 30m),
            Volume(3, "AZTECA", "ERBU", "Octubre 2026", 3, 45m),
            Volume(1, "LONGBALL", "UABU", "Agosto 2026", 1, 50m),
            Volume(2, "LONGBALL", "UABU", "Septiembre 2026", 2, 60m),
            Volume(3, "LONGBALL", "UABU", "Octubre 2026", 3, 90m));
        db.SmtFamilyRates.AddRange(
            Rate("AZTECA", 10m),
            Rate("LONGBALL", 20m));
        db.SmtLineAssignments.AddRange(
            Assignment(1, "AZTECA", "SMT 1", 1),
            Assignment(2, "AZTECA", "SMT 1", 1),
            Assignment(3, "AZTECA", "SMT 1", 1),
            Assignment(1, "LONGBALL", "SMT 2", 2),
            Assignment(2, "LONGBALL", "SMT 2", 2),
            Assignment(3, "LONGBALL", "SMT 2", 2));
        await db.SaveChangesAsync();
        var service = new SmtSummaryService(db, new SmtCapacityReportService(db));

        var summary = await service.GetSummaryAsync("Base FMD SMT");

        Assert.Collection(
            summary.Months,
            month => Assert.Equal("M1", month.WeekName),
            month => Assert.Equal("M2", month.WeekName),
            month => Assert.Equal("M3", month.WeekName));
        var erbuLinearity = Assert.Single(summary.LinearityRows, row => row.BusinessUnit == "ERBU");
        Assert.Equal(25m, erbuLinearity.MonthPercents["M1"]);
        Assert.Equal(30m, erbuLinearity.MonthPercents["M2"]);
        Assert.Equal(45m, erbuLinearity.MonthPercents["M3"]);
        var erbuProjection = Assert.Single(summary.BuildProjectionRows, row => row.BusinessUnit == "ERBU");
        Assert.Equal(100m, erbuProjection.TotalMps);
        Assert.Equal(25m, erbuProjection.MonthlyMps["M1"]);
        var smt1 = Assert.Single(summary.CapacityRows, row => row.LineCode == "SMT 1");
        Assert.True(smt1.Months["M1"].CapacityUnits > 0m);
        Assert.True(smt1.Months["M1"].UtilizationPercent > 0m);
        Assert.NotEmpty(summary.BottleneckRows);
    }

    private static ProductionWeekEntity Week(int id, string weekName) => new()
    {
        ProductionWeekId = id,
        ScenarioId = 1,
        WeekName = weekName,
        WorkDays = 5m,
        HoursPerShift = 10m,
        ShiftsPerDay = 1
    };

    private static OperationWeeklyCapacityEntity Capacity(int weekId) => new()
    {
        ScenarioId = 1,
        ProductionWeekId = weekId,
        OperationId = 1,
        WorkDays = 5m,
        HoursPerShift = 10m,
        ShiftsPerDay = 1,
        EquipmentQuantity = 1m
    };

    private static SmtFamilyVolumeEntity Volume(
        int weekId,
        string family,
        string businessUnit,
        string monthName,
        int monthSequence,
        decimal monthlyMps) => new()
    {
        ScenarioId = 1,
        ProductionWeekId = weekId,
        ProductFamily = family,
        BusinessUnit = businessUnit,
        MonthName = monthName,
        MonthSequence = monthSequence,
        MonthlyMps = monthlyMps,
        WeeklyMps = monthlyMps / (monthSequence == 3 ? 5m : 4m)
    };

    private static SmtFamilyRateEntity Rate(string family, decimal unitsPerHour) => new()
    {
        OperationId = 1,
        ProductFamily = family,
        UnitsPerHour = unitsPerHour,
        ChangeoverCount = 0m,
        BottleneckUnitsPerHour = unitsPerHour
    };

    private static SmtLineAssignmentEntity Assignment(
        int weekId,
        string family,
        string lineCode,
        int lineNumber) => new()
    {
        ScenarioId = 1,
        ProductionWeekId = weekId,
        OperationId = 1,
        ProductFamily = family,
        LineCode = lineCode,
        LineNumber = lineNumber,
        LineDistribution = 1m,
        MonthName = weekId == 1 ? "Agosto 2026" : weekId == 2 ? "Septiembre 2026" : "Octubre 2026",
        MonthSequence = weekId
    };

    private static PlanningDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PlanningDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new PlanningDbContext(options);
    }
}
