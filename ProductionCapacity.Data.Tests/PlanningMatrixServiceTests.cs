using Microsoft.EntityFrameworkCore;
using ProductionCapacity.Data;
using ProductionCapacity.Data.Entities;

namespace ProductionCapacity.Data.Tests;

public sealed class PlanningMatrixServiceTests
{
    [Fact]
    public async Task BuildsPlanUabuStyleMatrixForSelectedWeek()
    {
        await using var db = CreateContext();
        await SeedMatrixScenarioAsync(db);
        var service = new PlanningMatrixService(db);

        var matrix = await service.GetMatrixAsync("Base semanal");

        Assert.Equal("Base semanal", matrix.ScenarioName);
        Assert.Equal("WK13", matrix.WeekName);
        Assert.Collection(
            matrix.Operations,
            operation => Assert.Equal("ASSY", operation.OperationCode),
            operation => Assert.Equal("PACK", operation.OperationCode));

        var row = Assert.Single(matrix.Rows);
        Assert.Equal("Longball", row.PartNumber);
        Assert.Equal("Longball", row.ProductFamily);
        Assert.Equal(1_000m, row.DemandUnits);
        Assert.Equal(20.519m, row.Cells["ASSY"].LoadHours);
        Assert.Equal(27.359m, row.Cells["PACK"].LoadHours);
        var partRow = Assert.Single(row.ChildRows);
        Assert.Equal("68-103450", partRow.PartNumber);

        Assert.Equal(234.181m, matrix.OperationSummaries["ASSY"].RemainingHours);
        Assert.Equal(227.341m, matrix.OperationSummaries["PACK"].RemainingHours);
    }

    [Fact]
    public async Task GroupsDemandByFamilyAndKeepsPartNumberDrillDown()
    {
        await using var db = CreateContext();
        await SeedFamilyScenarioAsync(db);
        var service = new PlanningMatrixService(db);

        var matrix = await service.GetMatrixAsync("Base semanal");

        var family = Assert.Single(matrix.Rows);
        Assert.Equal("Azteca", family.ProductFamily);
        Assert.Equal("Azteca", family.PartNumber);
        Assert.Equal(300m, family.DemandUnits);
        Assert.Collection(
            family.ChildRows,
            part =>
            {
                Assert.Equal("68-A", part.PartNumber);
                Assert.Equal(100m, part.DemandUnits);
            },
            part =>
            {
                Assert.Equal("68-B", part.PartNumber);
                Assert.Equal(200m, part.DemandUnits);
            });
        Assert.Equal(10.001m, family.Cells["SMT"].LoadHours);
        Assert.Equal(3.334m, family.ChildRows[0].Cells["SMT"].LoadHours);
        Assert.Equal(6.667m, family.ChildRows[1].Cells["SMT"].LoadHours);
    }

    [Fact]
    public async Task IgnoresMaxCapacityWhenBuildingMatrixCapacity()
    {
        await using var db = CreateContext();
        await SeedMatrixScenarioAsync(db);
        var rate = await db.OperationRates.SingleAsync(item => item.OperationId == 1);
        rate.MaxCapacityUnits = 800m;
        await db.SaveChangesAsync();
        var service = new PlanningMatrixService(db);

        var matrix = await service.GetMatrixAsync("Base semanal");

        var cell = Assert.Single(matrix.Rows).Cells["ASSY"];
        Assert.Equal(12_412.886m, cell.TimeCapacityUnits);
        Assert.Equal(12_412.886m, cell.CapacityUnits);
        Assert.False(cell.IsConstrained);
        Assert.Null(cell.ConstraintName);
    }

    [Fact]
    public async Task KeepsPhysicalResourcesPendingAndDoesNotConstrainMatrixCapacity()
    {
        await using var db = CreateContext();
        await SeedMatrixScenarioAsync(db);
        db.ResourceTypes.Add(new ResourceTypeEntity
        {
            ResourceTypeId = 1,
            ResourceCode = "CHAMBER-LONG",
            ResourceName = "Chamber Longball",
            ResourceKind = "Chamber",
            IsActive = true
        });
        db.ResourceConfigurations.Add(new ResourceConfigurationEntity
        {
            ResourceConfigurationId = 1,
            ResourceTypeId = 1,
            OperationId = 1,
            ProductFamily = "Longball",
            UnitsPerResource = 7m,
            IsActive = true
        });
        db.ResourceInventories.Add(new ResourceInventoryEntity
        {
            ResourceInventoryId = 1,
            ScenarioId = 1,
            ProductionWeekId = 1,
            ResourceTypeId = 1,
            AvailableResourceCount = 100
        });
        await db.SaveChangesAsync();
        var service = new PlanningMatrixService(db);

        var matrix = await service.GetMatrixAsync("Base semanal");

        var cell = Assert.Single(matrix.Rows).Cells["ASSY"];
        Assert.Equal(12_412.886m, cell.CapacityUnits);
        Assert.Null(cell.ConstraintName);
        Assert.Null(cell.ConstraintKind);
        Assert.Null(cell.RequiredResourceCount);
        Assert.Null(cell.AvailableResourceCount);
        Assert.Null(cell.MissingResourceCount);
        Assert.False(cell.IsConstrained);
    }

    private static PlanningDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PlanningDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new PlanningDbContext(options);
    }

    private static async Task SeedMatrixScenarioAsync(PlanningDbContext db)
    {
        db.Scenarios.Add(new Scenario
        {
            ScenarioId = 1,
            ScenarioName = "Base semanal",
            Description = "Matrix test",
            IsBaseline = true
        });
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
            Description = "Longball sample",
            BusinessUnit = "UABU",
            ProductFamily = "Longball",
            DefaultYield = 1m
        });
        db.WeeklyDemand.Add(new WeeklyDemandEntity
        {
            WeeklyDemandId = 1,
            ScenarioId = 1,
            ProductionWeekId = 1,
            PartNumberId = 1,
            DemandUnits = 1_000m
        });
        db.Operations.AddRange(
            new PlanningOperation
            {
                OperationId = 1,
                OperationCode = "ASSY",
                OperationName = "ASSY",
                SequenceNumber = 1
            },
            new PlanningOperation
            {
                OperationId = 2,
                OperationCode = "PACK",
                OperationName = "PACK",
                SequenceNumber = 2
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
                EquipmentQuantity = 2m
            },
            new OperationWeeklyCapacityEntity
            {
                OperationWeeklyCapacityId = 2,
                ScenarioId = 1,
                ProductionWeekId = 1,
                OperationId = 2,
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
                PartNumberId = 1,
                OperationId = 2,
                StandardTimeSeconds = 98.4924m,
                YieldFactor = 1m,
                UtilizationFactor = 1m,
                EfficiencyFactor = 1m
            });

        await db.SaveChangesAsync();
    }

    private static async Task SeedFamilyScenarioAsync(PlanningDbContext db)
    {
        db.Scenarios.Add(new Scenario
        {
            ScenarioId = 1,
            ScenarioName = "Base semanal",
            IsBaseline = true
        });
        db.ProductionWeeks.Add(new ProductionWeekEntity
        {
            ProductionWeekId = 1,
            ScenarioId = 1,
            WeekName = "WK13",
            WorkDays = 5m,
            HoursPerShift = 8m,
            ShiftsPerDay = 1
        });
        db.PartNumbers.AddRange(
            new PartNumberEntity
            {
                PartNumberId = 1,
                PartNumber = "68-A",
                Description = "Azteca A",
                ProductFamily = "Azteca",
                DefaultYield = 1m
            },
            new PartNumberEntity
            {
                PartNumberId = 2,
                PartNumber = "68-B",
                Description = "Azteca B",
                ProductFamily = "Azteca",
                DefaultYield = 1m
            });
        db.WeeklyDemand.AddRange(
            new WeeklyDemandEntity
            {
                WeeklyDemandId = 1,
                ScenarioId = 1,
                ProductionWeekId = 1,
                PartNumberId = 1,
                DemandUnits = 100m
            },
            new WeeklyDemandEntity
            {
                WeeklyDemandId = 2,
                ScenarioId = 1,
                ProductionWeekId = 1,
                PartNumberId = 2,
                DemandUnits = 200m
            });
        db.Operations.Add(new PlanningOperation
        {
            OperationId = 1,
            OperationCode = "SMT",
            OperationName = "SMT",
            SequenceNumber = 1
        });
        db.OperationWeeklyCapacities.Add(new OperationWeeklyCapacityEntity
        {
            OperationWeeklyCapacityId = 1,
            ScenarioId = 1,
            ProductionWeekId = 1,
            OperationId = 1,
            WorkDays = 5m,
            HoursPerShift = 8m,
            ShiftsPerDay = 1,
            EquipmentQuantity = 1m
        });
        db.OperationRates.AddRange(
            new OperationRateEntity
            {
                OperationRateId = 1,
                PartNumberId = 1,
                OperationId = 1,
                StandardTimeSeconds = 120m,
                YieldFactor = 1m,
                UtilizationFactor = 1m,
                EfficiencyFactor = 1m
            },
            new OperationRateEntity
            {
                OperationRateId = 2,
                PartNumberId = 2,
                OperationId = 1,
                StandardTimeSeconds = 120m,
                YieldFactor = 1m,
                UtilizationFactor = 1m,
                EfficiencyFactor = 1m
            });

        await db.SaveChangesAsync();
    }
}
