using Microsoft.EntityFrameworkCore;
using ProductionCapacity.Data;
using ProductionCapacity.Data.Entities;

namespace ProductionCapacity.Data.Tests;

public sealed class ParityComparisonServiceTests
{
    [Fact]
    public async Task ComparesPlanUabuSamplesAgainstCalculatedMatrix()
    {
        await using var db = CreateContext();
        await SeedPlanUabuLikeMatrixAsync(db);
        var comparison = new ParityComparisonService(new PlanningMatrixService(db));

        var result = await comparison.ComparePlanUabuAsync();

        Assert.True(result.IsPassing);
        var row = Assert.Single(result.Rows);
        Assert.Equal(20.519m, row.ActualLoadHours);
        Assert.Equal(254.700m, row.ActualAvailableHours);
        Assert.Equal(228.350m, row.ActualRemainingHours);
    }

    private static PlanningDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PlanningDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new PlanningDbContext(options);
    }

    private static async Task SeedPlanUabuLikeMatrixAsync(PlanningDbContext db)
    {
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
                PartNumber = "68-103450",
                Description = "Longball",
                DefaultYield = 1m
            },
            new PartNumberEntity
            {
                PartNumberId = 2,
                PartNumber = "68-OTHER",
                Description = "Carga adicional",
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
                ProductionWeekId = 1,
                PartNumberId = 2,
                DemandUnits = 100m
            });
        db.Operations.Add(new PlanningOperation
        {
            OperationId = 1,
            OperationCode = "ASSY",
            OperationName = "ASSY",
            SequenceNumber = 1
        });
        db.OperationWeeklyCapacities.Add(new OperationWeeklyCapacityEntity
        {
            OperationWeeklyCapacityId = 1,
            ScenarioId = 1,
            ProductionWeekId = 1,
            OperationId = 1,
            WorkDays = 5.66m,
            HoursPerShift = 7.5m,
            ShiftsPerDay = 3,
            EquipmentQuantity = 2m
        });
        db.OperationRates.AddRange(
            new OperationRateEntity
            {
                OperationRateId = 1,
                PartNumberId = 1,
                OperationId = 1,
                StandardTimeSeconds = 73.8684m,
                YieldFactor = 1m,
                UtilizationFactor = 1m,
                EfficiencyFactor = 1m
            },
            new OperationRateEntity
            {
                OperationRateId = 2,
                PartNumberId = 2,
                OperationId = 1,
                StandardTimeSeconds = 209.916m,
                YieldFactor = 1m,
                UtilizationFactor = 1m,
                EfficiencyFactor = 1m
            });

        await db.SaveChangesAsync();
    }
}
