using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ProductionCapacity.Data;
using ProductionCapacity.Data.Entities;

namespace ProductionCapacity.Data.Tests;

public sealed class CatalogAdministrationServiceTests
{
    [Fact]
    public async Task SavesAndUpdatesMachineProductRackAndAccessoryCatalogs()
    {
        var factory = CreateFactory();
        await SeedSmtFamilyAsync(factory, "FC");
        var service = new CatalogAdministrationService(factory);

        var machineId = await service.SaveMachineAsync(new MachineCatalogItem
        {
            MachineCode = "ASSY-2",
            MachineName = "Assembly 2",
            MachineType = "Assembly",
            DefaultUtilizationFactor = 0.80m,
            IsActive = true
        });
        var partId = await service.SavePartNumberAsync(new PartNumberCatalogItem
        {
            PartNumber = "68-1000",
            Description = "Producto nuevo",
            BusinessUnit = "ERBU",
            ProductFamily = "FC",
            SubFamily = "FC3",
            DefaultYield = 0.95m,
            IsActive = true
        });
        var rackTypeId = await service.SaveRackTypeAsync(new RackTypeCatalogItem
        {
            RackCode = "CARRO-7",
            RackName = "Carro 7 posiciones",
            BaseUnitsPerRack = 7m,
            IsActive = true
        });
        var accessoryId = await service.SaveRackAccessoryAsync(new RackAccessoryCatalogItem
        {
            AccessoryCode = "EXT-3",
            AccessoryName = "Extension 3 posiciones",
            AdditionalUnitsPerRack = 3m,
            IsActive = true
        });

        await service.SaveMachineAsync(new MachineCatalogItem
        {
            MachineId = machineId,
            MachineCode = "ASSY-2",
            MachineName = "Assembly 2 editada",
            MachineType = "Assembly",
            DefaultUtilizationFactor = 0.75m,
            IsActive = true
        });

        var snapshot = await service.GetSnapshotAsync();

        Assert.Equal("Assembly 2 editada", Assert.Single(snapshot.Machines).MachineName);
        Assert.Equal(0.75m, Assert.Single(snapshot.Machines).DefaultUtilizationFactor);
        Assert.Equal(partId, Assert.Single(snapshot.PartNumbers).PartNumberId);
        Assert.Equal(rackTypeId, Assert.Single(snapshot.RackTypes).RackTypeId);
        Assert.Equal(accessoryId, Assert.Single(snapshot.RackAccessories).RackAccessoryId);
    }

    [Fact]
    public async Task SavesSubFamilyAndRequiresItForSmtPartNumbers()
    {
        var factory = CreateFactory();
        await SeedSmtFamilyAsync(factory, "AZTECA");
        var service = new CatalogAdministrationService(factory);

        var subFamilyId = await service.SaveSubFamilyAsync(new SubFamilyCatalogItem
        {
            ProductFamily = "azteca",
            SubFamilyName = "fc3",
            IsActive = true
        });

        var partId = await service.SavePartNumberAsync(new PartNumberCatalogItem
        {
            PartNumber = "68-1000",
            Description = "Producto SMT",
            BusinessUnit = "ERBU",
            ProductFamily = "azteca",
            SubFamily = "fc3",
            DefaultYield = 0.95m,
            IsActive = true
        });

        var snapshot = await service.GetSnapshotAsync();

        var subFamily = Assert.Single(snapshot.SubFamilies, item => item.SubFamilyName == "FC3");
        Assert.Equal(subFamilyId, subFamily.SubFamilyId);
        Assert.Equal("AZTECA", subFamily.ProductFamily);
        Assert.Equal("FC3", subFamily.SubFamilyName);
        var part = Assert.Single(snapshot.PartNumbers);
        Assert.Equal(partId, part.PartNumberId);
        Assert.Equal("AZTECA", part.ProductFamily);
        Assert.Equal("FC3", part.SubFamily);
    }

    [Fact]
    public async Task RejectsSmtPartNumberWhenSubFamilyDoesNotExist()
    {
        var factory = CreateFactory();
        await SeedSmtFamilyAsync(factory, "AZTECA");
        var service = new CatalogAdministrationService(factory);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SavePartNumberAsync(new PartNumberCatalogItem
            {
                PartNumber = "68-1000",
                Description = "Producto SMT",
                BusinessUnit = "ERBU",
                ProductFamily = "AZTECA",
                SubFamily = "FC3",
                DefaultYield = 0.95m,
                IsActive = true
            }));

        Assert.Contains("subfamilia", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SavesRouteAndRackConfigurationWithLookupNames()
    {
        var factory = CreateFactory();
        await SeedLookupsAsync(factory);
        var service = new CatalogAdministrationService(factory);

        await service.SaveProcessRouteAsync(new ProcessRouteCatalogItem
        {
            PartNumberId = 1,
            MachineId = 1,
            OperationName = "Camara",
            SequenceNumber = 10,
            StandardTimeSeconds = 300m,
            DefaultUtilizationFactor = 0.70m,
            DefaultYield = 0.90m,
            IsCapacityConstrained = true
        });
        await service.SaveRackConfigurationAsync(new RackConfigurationCatalogItem
        {
            RackTypeId = 1,
            RackAccessoryId = 1,
            MachineId = 1,
            PartNumberId = 1,
            UnitsPerRack = 10m,
            Notes = "Carro con extension",
            IsActive = true
        });

        var snapshot = await service.GetSnapshotAsync();

        var route = Assert.Single(snapshot.ProcessRoutes);
        Assert.Equal("68-2000", route.PartNumber);
        Assert.Equal("CHAMBER 1", route.MachineName);

        var rackConfiguration = Assert.Single(snapshot.RackConfigurations);
        Assert.Equal("Carro base", rackConfiguration.RackName);
        Assert.Equal("Extension", rackConfiguration.AccessoryName);
        Assert.Equal(10m, rackConfiguration.UnitsPerRack);
    }

    private static IDbContextFactory<PlanningDbContext> CreateFactory()
    {
        var options = new DbContextOptionsBuilder<PlanningDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new PooledDbContextFactory<PlanningDbContext>(options);
    }

    private static async Task SeedLookupsAsync(IDbContextFactory<PlanningDbContext> factory)
    {
        await using var db = await factory.CreateDbContextAsync();
        db.Machines.Add(new MachineEntity
        {
            MachineId = 1,
            MachineCode = "CH-1",
            MachineName = "CHAMBER 1",
            MachineType = "Chamber",
            DefaultUtilizationFactor = 0.80m
        });
        db.PartNumbers.Add(new PartNumberEntity
        {
            PartNumberId = 1,
            PartNumber = "68-2000",
            Description = "Producto camara",
            DefaultYield = 0.95m
        });
        db.RackTypes.Add(new RackType
        {
            RackTypeId = 1,
            RackCode = "CARRO",
            RackName = "Carro base",
            BaseUnitsPerRack = 7m
        });
        db.RackAccessories.Add(new RackAccessory
        {
            RackAccessoryId = 1,
            AccessoryCode = "EXT",
            AccessoryName = "Extension",
            AdditionalUnitsPerRack = 3m
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedSmtFamilyAsync(IDbContextFactory<PlanningDbContext> factory, string productFamily)
    {
        await using var db = await factory.CreateDbContextAsync();
        db.SmtFamilies.Add(new SmtFamilyEntity
        {
            ProductFamily = productFamily,
            BusinessUnit = "ERBU",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        db.SubFamilies.Add(new SubFamilyEntity
        {
            ProductFamily = productFamily,
            SubFamilyName = $"{productFamily}3",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }
}
