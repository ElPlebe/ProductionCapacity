using Microsoft.EntityFrameworkCore;
using ProductionCapacity.Data;
using ProductionCapacity.Data.Entities;

namespace ProductionCapacity.Data.Tests;

public sealed class CapacityPlanServiceTests
{
    [Fact]
    public async Task LoadsScenarioPlanAndAppliesMachinePartOverridesFirst()
    {
        await using var db = CreateContext();
        await SeedScenarioAsync(db);
        var service = new CapacityPlanService(db);

        var plan = await service.GetPlanAsync("Base semanal");

        Assert.Equal("Base semanal", plan.ScenarioName);
        Assert.Equal("WK13", plan.Week.Name);
        var row = Assert.Single(plan.Rows);
        Assert.Equal("ASSY 1", row.MachineName);
        Assert.Equal("68-6792", row.PartNumber);
        Assert.Equal(60m, row.UtilizationPercent);
        Assert.Equal(65m, row.YieldPercent);
        Assert.Equal(240m, row.StandardTimeSeconds);
        Assert.Equal(1_000m, row.DemandUnits);

        var result = Assert.Single(plan.Results);
        Assert.Equal(0.60m, result.AppliedUtilizationFactor);
        Assert.Equal(0.65m, result.AppliedYield);
        Assert.Equal(745.005m, result.CapacityUnits);
    }

    [Fact]
    public async Task LoadsRackConstraintAndAppliesItToResults()
    {
        await using var db = CreateContext();
        await SeedScenarioAsync(db);

        db.RackTypes.Add(new RackType
        {
            RackTypeId = 1,
            RackCode = "CARRO-BASE",
            RackName = "Carro base",
            BaseUnitsPerRack = 7m
        });
        db.RackConfigurations.Add(new RackConfiguration
        {
            RackConfigurationId = 1,
            RackTypeId = 1,
            MachineId = 1,
            PartNumberId = 1,
            UnitsPerRack = 7m
        });
        db.ScenarioRackInventories.Add(new ScenarioRackInventory
        {
            ScenarioRackInventoryId = 1,
            ScenarioId = 1,
            ProductionWeekId = 1,
            MachineId = 1,
            RackTypeId = 1,
            AvailableRackCount = 1
        });
        var demand = await db.WeeklyDemand.FindAsync(1);
        demand!.DemandUnits = 14m;
        await db.SaveChangesAsync();

        var service = new CapacityPlanService(db);

        var plan = await service.GetPlanAsync("Base semanal");

        var row = Assert.Single(plan.Rows);
        Assert.Equal("Carro base", row.RackName);
        Assert.Equal(7m, row.UnitsPerRack);
        Assert.Equal(1, row.AvailableRackCount);

        var result = Assert.Single(plan.Results);
        Assert.Equal(7m, result.RackCapacityUnits);
        Assert.Equal(7m, result.CapacityUnits);
        Assert.True(result.IsRackConstrained);
        Assert.Equal(1, result.MissingRackCount);
    }

    private static PlanningDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PlanningDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new PlanningDbContext(options);
    }

    private static async Task SeedScenarioAsync(PlanningDbContext db)
    {
        var scenario = new Scenario
        {
            ScenarioId = 1,
            ScenarioName = "Base semanal",
            Description = "Test",
            IsBaseline = true
        };
        var week = new ProductionWeekEntity
        {
            ProductionWeekId = 1,
            ScenarioId = 1,
            WeekName = "WK13",
            WorkDays = 5.66m,
            HoursPerShift = 7.5m,
            ShiftsPerDay = 3,
            HolidayDays = 0,
            OvertimeHours = 0,
            PlannedDowntimeHours = 0
        };
        var machine = new MachineEntity
        {
            MachineId = 1,
            MachineCode = "ASSY-1",
            MachineName = "ASSY 1",
            MachineType = "Assembly",
            DefaultUtilizationFactor = 0.80m
        };
        var part = new PartNumberEntity
        {
            PartNumberId = 1,
            PartNumber = "68-6792",
            Description = "FC3",
            BusinessUnit = "ERBU",
            ProductFamily = "FC3",
            DefaultYield = 0.95m
        };
        var route = new ProcessRoute
        {
            ProcessRouteId = 1,
            PartNumberId = 1,
            MachineId = 1,
            OperationName = "ASSY",
            SequenceNumber = 1,
            StandardTimeSeconds = 300m
        };
        var demand = new WeeklyDemandEntity
        {
            WeeklyDemandId = 1,
            ScenarioId = 1,
            ProductionWeekId = 1,
            PartNumberId = 1,
            DemandUnits = 1_000m
        };
        var machineOverride = new ScenarioMachineOverride
        {
            ScenarioMachineOverrideId = 1,
            ScenarioId = 1,
            MachineId = 1,
            UtilizationFactorOverride = 0.75m
        };
        var partOverride = new ScenarioPartOverride
        {
            ScenarioPartOverrideId = 1,
            ScenarioId = 1,
            PartNumberId = 1,
            YieldOverride = 0.70m
        };
        var machinePartOverride = new ScenarioMachinePartOverride
        {
            ScenarioMachinePartOverrideId = 1,
            ScenarioId = 1,
            MachineId = 1,
            PartNumberId = 1,
            UtilizationFactorOverride = 0.60m,
            YieldOverride = 0.65m,
            StandardTimeSecondsOverride = 240m
        };

        db.Scenarios.Add(scenario);
        db.ProductionWeeks.Add(week);
        db.Machines.Add(machine);
        db.PartNumbers.Add(part);
        db.ProcessRoutes.Add(route);
        db.WeeklyDemand.Add(demand);
        db.ScenarioMachineOverrides.Add(machineOverride);
        db.ScenarioPartOverrides.Add(partOverride);
        db.ScenarioMachinePartOverrides.Add(machinePartOverride);
        await db.SaveChangesAsync();
    }
}
