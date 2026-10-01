using Microsoft.EntityFrameworkCore;
using ProductionCapacity.Data.Entities;
using ProductionCapacity.Engine;

namespace ProductionCapacity.Data;

public sealed class CatalogAdministrationService(IDbContextFactory<PlanningDbContext> dbFactory)
{
    public async Task<CatalogSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var machines = await db.Machines
            .AsNoTracking()
            .OrderBy(item => item.MachineCode)
            .Select(item => new MachineCatalogItem
            {
                MachineId = item.MachineId,
                MachineCode = item.MachineCode,
                MachineName = item.MachineName,
                MachineType = item.MachineType,
                DefaultUtilizationFactor = item.DefaultUtilizationFactor,
                IsActive = item.IsActive
            })
            .ToListAsync(cancellationToken);

        var parts = await db.PartNumbers
            .AsNoTracking()
            .OrderBy(item => item.PartNumber)
            .Select(item => new PartNumberCatalogItem
            {
                PartNumberId = item.PartNumberId,
                PartNumber = item.PartNumber,
                Description = item.Description,
                BusinessUnit = item.BusinessUnit,
                ProductFamily = item.ProductFamily,
                SubFamily = item.SubFamily,
                DefaultYield = item.DefaultYield,
                IsActive = item.IsActive
            })
            .ToListAsync(cancellationToken);

        var subFamilies = await db.SubFamilies
            .AsNoTracking()
            .OrderBy(item => item.ProductFamily)
            .ThenBy(item => item.SubFamilyName)
            .Select(item => new SubFamilyCatalogItem
            {
                SubFamilyId = item.SubFamilyId,
                ProductFamily = item.ProductFamily,
                SubFamilyName = item.SubFamilyName,
                Notes = item.Notes,
                IsActive = item.IsActive
            })
            .ToListAsync(cancellationToken);

        var smtFamilies = await db.SmtFamilies
            .AsNoTracking()
            .OrderBy(item => item.ProductFamily)
            .Select(item => new SmtFamilyLookupCatalogItem
            {
                ProductFamily = item.ProductFamily,
                BusinessUnit = item.BusinessUnit,
                IsActive = item.IsActive
            })
            .ToListAsync(cancellationToken);

        var rackTypes = await db.RackTypes
            .AsNoTracking()
            .OrderBy(item => item.RackCode)
            .Select(item => new RackTypeCatalogItem
            {
                RackTypeId = item.RackTypeId,
                RackCode = item.RackCode,
                RackName = item.RackName,
                BaseUnitsPerRack = item.BaseUnitsPerRack,
                IsActive = item.IsActive
            })
            .ToListAsync(cancellationToken);

        var rackAccessories = await db.RackAccessories
            .AsNoTracking()
            .OrderBy(item => item.AccessoryCode)
            .Select(item => new RackAccessoryCatalogItem
            {
                RackAccessoryId = item.RackAccessoryId,
                AccessoryCode = item.AccessoryCode,
                AccessoryName = item.AccessoryName,
                AdditionalUnitsPerRack = item.AdditionalUnitsPerRack,
                IsActive = item.IsActive
            })
            .ToListAsync(cancellationToken);

        var routes = await (
            from route in db.ProcessRoutes.AsNoTracking()
            join part in db.PartNumbers.AsNoTracking() on route.PartNumberId equals part.PartNumberId
            join machine in db.Machines.AsNoTracking() on route.MachineId equals machine.MachineId
            orderby part.PartNumber, route.SequenceNumber
            select new ProcessRouteCatalogItem
            {
                ProcessRouteId = route.ProcessRouteId,
                PartNumberId = route.PartNumberId,
                MachineId = route.MachineId,
                OperationName = route.OperationName,
                SequenceNumber = route.SequenceNumber,
                StandardTimeSeconds = route.StandardTimeSeconds,
                DefaultUtilizationFactor = route.DefaultUtilizationFactor,
                DefaultYield = route.DefaultYield,
                IsCapacityConstrained = route.IsCapacityConstrained,
                PartNumber = part.PartNumber,
                MachineName = machine.MachineName
            })
            .ToListAsync(cancellationToken);

        var rackConfigurations = await (
            from configuration in db.RackConfigurations.AsNoTracking()
            join rack in db.RackTypes.AsNoTracking() on configuration.RackTypeId equals rack.RackTypeId
            join part in db.PartNumbers.AsNoTracking() on configuration.PartNumberId equals part.PartNumberId
            join machine in db.Machines.AsNoTracking() on configuration.MachineId equals machine.MachineId
            join accessoryOptional in db.RackAccessories.AsNoTracking()
                on configuration.RackAccessoryId equals accessoryOptional.RackAccessoryId into accessories
            from accessory in accessories.DefaultIfEmpty()
            orderby part.PartNumber, machine.MachineName, rack.RackCode
            select new RackConfigurationCatalogItem
            {
                RackConfigurationId = configuration.RackConfigurationId,
                RackTypeId = configuration.RackTypeId,
                RackAccessoryId = configuration.RackAccessoryId,
                MachineId = configuration.MachineId,
                PartNumberId = configuration.PartNumberId,
                UnitsPerRack = configuration.UnitsPerRack,
                Notes = configuration.Notes,
                IsActive = configuration.IsActive,
                RackName = rack.RackName,
                AccessoryName = accessory == null ? null : accessory.AccessoryName,
                PartNumber = part.PartNumber,
                MachineName = machine.MachineName
            })
            .ToListAsync(cancellationToken);

        var crossReferences = await (
            from crossReference in db.PartCrossReferences.AsNoTracking()
            join part in db.PartNumbers.AsNoTracking() on crossReference.PartNumberId equals part.PartNumberId
            orderby part.PartNumber, crossReference.ReferenceType, crossReference.ReferenceValue
            select new PartCrossReferenceCatalogItem
            {
                PartCrossReferenceId = crossReference.PartCrossReferenceId,
                PartNumberId = crossReference.PartNumberId,
                ReferenceType = crossReference.ReferenceType,
                ReferenceValue = crossReference.ReferenceValue,
                PartNumber = part.PartNumber
            })
            .ToListAsync(cancellationToken);

        var operations = await db.Operations
            .AsNoTracking()
            .OrderBy(item => item.SequenceNumber)
            .ThenBy(item => item.OperationCode)
            .Select(item => new OperationCatalogItem
            {
                OperationId = item.OperationId,
                OperationCode = item.OperationCode,
                OperationName = item.OperationName,
                OperationFamily = item.OperationFamily,
                SequenceNumber = item.SequenceNumber,
                IsActive = item.IsActive
            })
            .ToListAsync(cancellationToken);

        var testTimes = await (
            from testTime in db.TestTimeDefinitions.AsNoTracking()
            join part in db.PartNumbers.AsNoTracking() on testTime.PartNumberId equals part.PartNumberId
            join operation in db.Operations.AsNoTracking() on testTime.OperationId equals operation.OperationId
            orderby part.PartNumber, operation.SequenceNumber, testTime.TestName
            select new TestTimeDefinitionCatalogItem
            {
                TestTimeDefinitionId = testTime.TestTimeDefinitionId,
                PartNumberId = testTime.PartNumberId,
                OperationId = testTime.OperationId,
                TestName = testTime.TestName,
                TestTimeMinutes = testTime.TestTimeMinutes,
                HandlingMinutes = testTime.HandlingMinutes,
                QuantityCell = testTime.QuantityCell,
                QuantityEquipment = testTime.QuantityEquipment,
                ManualStandardTimeSeconds = testTime.ManualStandardTimeSeconds,
                CalculatedStandardTimeSeconds = testTime.CalculatedStandardTimeSeconds,
                IsActive = testTime.IsActive,
                PartNumber = part.PartNumber,
                OperationCode = operation.OperationCode
            })
            .ToListAsync(cancellationToken);

        var resourceTypes = await db.ResourceTypes
            .AsNoTracking()
            .OrderBy(item => item.ResourceKind)
            .ThenBy(item => item.ResourceCode)
            .Select(item => new ResourceTypeCatalogItem
            {
                ResourceTypeId = item.ResourceTypeId,
                ResourceCode = item.ResourceCode,
                ResourceName = item.ResourceName,
                ResourceKind = item.ResourceKind,
                IsActive = item.IsActive
            })
            .ToListAsync(cancellationToken);

        return new CatalogSnapshot(
            machines,
            parts,
            subFamilies,
            smtFamilies,
            rackTypes,
            rackAccessories,
            routes,
            rackConfigurations,
            crossReferences,
            operations,
            testTimes,
            resourceTypes);
    }

    public async Task<int> SaveMachineAsync(MachineCatalogItem item, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = item.MachineId == 0
            ? new MachineEntity()
            : await db.Machines.SingleAsync(machine => machine.MachineId == item.MachineId, cancellationToken);

        entity.MachineCode = Required(item.MachineCode);
        entity.MachineName = Required(item.MachineName);
        entity.MachineType = EmptyToNull(item.MachineType);
        entity.DefaultUtilizationFactor = item.DefaultUtilizationFactor;
        entity.IsActive = item.IsActive;

        if (item.MachineId == 0)
        {
            db.Machines.Add(entity);
        }

        await db.SaveChangesAsync(cancellationToken);
        return entity.MachineId;
    }

    public async Task<int> SavePartNumberAsync(PartNumberCatalogItem item, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = item.PartNumberId == 0
            ? new PartNumberEntity()
            : await db.PartNumbers.SingleAsync(part => part.PartNumberId == item.PartNumberId, cancellationToken);

        var productFamily = EmptyToNull(item.ProductFamily)?.ToUpperInvariant();
        var subFamily = EmptyToNull(item.SubFamily)?.ToUpperInvariant();
        var isSmtFamily = productFamily is not null && await db.SmtFamilies.AnyAsync(family =>
            family.ProductFamily == productFamily &&
            family.IsActive,
            cancellationToken);

        if (isSmtFamily)
        {
            if (subFamily is null)
            {
                throw new InvalidOperationException("La subfamilia es obligatoria para numeros de parte SMT.");
            }

            var subFamilyExists = await db.SubFamilies.AnyAsync(existing =>
                existing.ProductFamily == productFamily &&
                existing.SubFamilyName == subFamily &&
                existing.IsActive,
                cancellationToken);
            if (!subFamilyExists)
            {
                throw new InvalidOperationException($"La subfamilia '{subFamily}' no existe para la familia SMT '{productFamily}'.");
            }
        }

        entity.PartNumber = Required(item.PartNumber);
        entity.Description = Required(item.Description);
        entity.BusinessUnit = EmptyToNull(item.BusinessUnit);
        entity.ProductFamily = productFamily;
        entity.SubFamily = subFamily;
        entity.DefaultYield = item.DefaultYield;
        entity.IsActive = item.IsActive;

        if (item.PartNumberId == 0)
        {
            db.PartNumbers.Add(entity);
        }

        await db.SaveChangesAsync(cancellationToken);
        return entity.PartNumberId;
    }

    public async Task<int> SaveSubFamilyAsync(SubFamilyCatalogItem item, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var productFamily = Required(item.ProductFamily).ToUpperInvariant();
        var subFamilyName = Required(item.SubFamilyName).ToUpperInvariant();
        var familyExists = await db.SmtFamilies.AnyAsync(family =>
            family.ProductFamily == productFamily &&
            family.IsActive,
            cancellationToken);
        if (!familyExists)
        {
            throw new InvalidOperationException($"La familia SMT '{productFamily}' no existe o esta inactiva.");
        }

        var duplicate = await db.SubFamilies.AnyAsync(subFamily =>
            subFamily.SubFamilyId != item.SubFamilyId &&
            subFamily.ProductFamily == productFamily &&
            subFamily.SubFamilyName == subFamilyName,
            cancellationToken);
        if (duplicate)
        {
            throw new InvalidOperationException($"La subfamilia '{subFamilyName}' ya existe para '{productFamily}'.");
        }

        var now = DateTime.UtcNow;
        var entity = item.SubFamilyId == 0
            ? new SubFamilyEntity { CreatedAtUtc = now }
            : await db.SubFamilies.SingleAsync(subFamily => subFamily.SubFamilyId == item.SubFamilyId, cancellationToken);

        entity.ProductFamily = productFamily;
        entity.SubFamilyName = subFamilyName;
        entity.Notes = EmptyToNull(item.Notes);
        entity.IsActive = item.IsActive;
        entity.UpdatedAtUtc = now;

        if (item.SubFamilyId == 0)
        {
            db.SubFamilies.Add(entity);
        }

        await db.SaveChangesAsync(cancellationToken);
        return entity.SubFamilyId;
    }

    public async Task<int> SaveRackTypeAsync(RackTypeCatalogItem item, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = item.RackTypeId == 0
            ? new RackType()
            : await db.RackTypes.SingleAsync(rack => rack.RackTypeId == item.RackTypeId, cancellationToken);

        entity.RackCode = Required(item.RackCode);
        entity.RackName = Required(item.RackName);
        entity.BaseUnitsPerRack = item.BaseUnitsPerRack;
        entity.IsActive = item.IsActive;

        if (item.RackTypeId == 0)
        {
            db.RackTypes.Add(entity);
        }

        await db.SaveChangesAsync(cancellationToken);
        return entity.RackTypeId;
    }

    public async Task<int> SaveRackAccessoryAsync(RackAccessoryCatalogItem item, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = item.RackAccessoryId == 0
            ? new RackAccessory()
            : await db.RackAccessories.SingleAsync(accessory => accessory.RackAccessoryId == item.RackAccessoryId, cancellationToken);

        entity.AccessoryCode = Required(item.AccessoryCode);
        entity.AccessoryName = Required(item.AccessoryName);
        entity.AdditionalUnitsPerRack = item.AdditionalUnitsPerRack;
        entity.IsActive = item.IsActive;

        if (item.RackAccessoryId == 0)
        {
            db.RackAccessories.Add(entity);
        }

        await db.SaveChangesAsync(cancellationToken);
        return entity.RackAccessoryId;
    }

    public async Task<int> SaveProcessRouteAsync(ProcessRouteCatalogItem item, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = item.ProcessRouteId == 0
            ? new ProcessRoute()
            : await db.ProcessRoutes.SingleAsync(route => route.ProcessRouteId == item.ProcessRouteId, cancellationToken);

        entity.PartNumberId = item.PartNumberId;
        entity.MachineId = item.MachineId;
        entity.OperationName = Required(item.OperationName);
        entity.SequenceNumber = item.SequenceNumber;
        entity.StandardTimeSeconds = item.StandardTimeSeconds;
        entity.DefaultUtilizationFactor = item.DefaultUtilizationFactor;
        entity.DefaultYield = item.DefaultYield;
        entity.IsCapacityConstrained = item.IsCapacityConstrained;

        if (item.ProcessRouteId == 0)
        {
            db.ProcessRoutes.Add(entity);
        }

        await db.SaveChangesAsync(cancellationToken);
        return entity.ProcessRouteId;
    }

    public async Task<int> SaveRackConfigurationAsync(RackConfigurationCatalogItem item, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = item.RackConfigurationId == 0
            ? new RackConfiguration()
            : await db.RackConfigurations.SingleAsync(configuration => configuration.RackConfigurationId == item.RackConfigurationId, cancellationToken);

        entity.RackTypeId = item.RackTypeId;
        entity.RackAccessoryId = item.RackAccessoryId == 0 ? null : item.RackAccessoryId;
        entity.MachineId = item.MachineId;
        entity.PartNumberId = item.PartNumberId;
        entity.UnitsPerRack = item.UnitsPerRack;
        entity.Notes = EmptyToNull(item.Notes);
        entity.IsActive = item.IsActive;

        if (item.RackConfigurationId == 0)
        {
            db.RackConfigurations.Add(entity);
        }

        await db.SaveChangesAsync(cancellationToken);
        return entity.RackConfigurationId;
    }

    public async Task<int> SavePartCrossReferenceAsync(PartCrossReferenceCatalogItem item, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var referenceType = Required(item.ReferenceType).ToUpperInvariant();
        var referenceValue = Required(item.ReferenceValue);
        var duplicateExists = await db.PartCrossReferences.AnyAsync(reference =>
            reference.PartCrossReferenceId != item.PartCrossReferenceId &&
            reference.ReferenceType == referenceType &&
            reference.ReferenceValue == referenceValue,
            cancellationToken);

        if (duplicateExists)
        {
            throw new InvalidOperationException($"La referencia ya existe: {referenceType} {referenceValue}.");
        }

        var entity = item.PartCrossReferenceId == 0
            ? new PartCrossReference()
            : await db.PartCrossReferences.SingleAsync(reference => reference.PartCrossReferenceId == item.PartCrossReferenceId, cancellationToken);

        entity.PartNumberId = item.PartNumberId;
        entity.ReferenceType = referenceType;
        entity.ReferenceValue = referenceValue;

        if (item.PartCrossReferenceId == 0)
        {
            db.PartCrossReferences.Add(entity);
        }

        await db.SaveChangesAsync(cancellationToken);
        return entity.PartCrossReferenceId;
    }

    public async Task<int> SaveTestTimeDefinitionAsync(TestTimeDefinitionCatalogItem item, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = item.TestTimeDefinitionId == 0
            ? new TestTimeDefinitionEntity()
            : await db.TestTimeDefinitions.SingleAsync(testTime => testTime.TestTimeDefinitionId == item.TestTimeDefinitionId, cancellationToken);

        entity.PartNumberId = item.PartNumberId;
        entity.OperationId = item.OperationId;
        entity.TestName = Required(item.TestName);
        entity.TestTimeMinutes = item.TestTimeMinutes;
        entity.HandlingMinutes = item.HandlingMinutes;
        entity.QuantityCell = item.QuantityCell;
        entity.QuantityEquipment = item.QuantityEquipment;
        entity.ManualStandardTimeSeconds = item.ManualStandardTimeSeconds;
        entity.CalculatedStandardTimeSeconds = TestTimeCalculator.CalculateStandardTimeSeconds(new TestTimeDefinition(
            item.TestTimeMinutes,
            item.HandlingMinutes,
            item.QuantityCell,
            item.QuantityEquipment,
            item.ManualStandardTimeSeconds));
        entity.IsActive = item.IsActive;

        if (item.TestTimeDefinitionId == 0)
        {
            db.TestTimeDefinitions.Add(entity);
        }

        await db.SaveChangesAsync(cancellationToken);
        return entity.TestTimeDefinitionId;
    }

    public async Task<int> SaveResourceTypeAsync(ResourceTypeCatalogItem item, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = item.ResourceTypeId == 0
            ? new ResourceTypeEntity()
            : await db.ResourceTypes.SingleAsync(resource => resource.ResourceTypeId == item.ResourceTypeId, cancellationToken);

        entity.ResourceCode = Required(item.ResourceCode).ToUpperInvariant();
        entity.ResourceName = Required(item.ResourceName);
        entity.ResourceKind = Required(item.ResourceKind);
        entity.IsActive = item.IsActive;

        if (item.ResourceTypeId == 0)
        {
            db.ResourceTypes.Add(entity);
        }

        await db.SaveChangesAsync(cancellationToken);
        return entity.ResourceTypeId;
    }

    private static string Required(string value) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("El campo es requerido.") : value.Trim();

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record CatalogSnapshot(
    IReadOnlyList<MachineCatalogItem> Machines,
    IReadOnlyList<PartNumberCatalogItem> PartNumbers,
    IReadOnlyList<SubFamilyCatalogItem> SubFamilies,
    IReadOnlyList<SmtFamilyLookupCatalogItem> SmtFamilies,
    IReadOnlyList<RackTypeCatalogItem> RackTypes,
    IReadOnlyList<RackAccessoryCatalogItem> RackAccessories,
    IReadOnlyList<ProcessRouteCatalogItem> ProcessRoutes,
    IReadOnlyList<RackConfigurationCatalogItem> RackConfigurations,
    IReadOnlyList<PartCrossReferenceCatalogItem> PartCrossReferences,
    IReadOnlyList<OperationCatalogItem> Operations,
    IReadOnlyList<TestTimeDefinitionCatalogItem> TestTimeDefinitions,
    IReadOnlyList<ResourceTypeCatalogItem> ResourceTypes);

public sealed class MachineCatalogItem
{
    public int MachineId { get; set; }
    public string MachineCode { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public string? MachineType { get; set; }
    public decimal DefaultUtilizationFactor { get; set; } = 0.80m;
    public bool IsActive { get; set; } = true;
}

public sealed class PartNumberCatalogItem
{
    public int PartNumberId { get; set; }
    public string PartNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? BusinessUnit { get; set; }
    public string? ProductFamily { get; set; }
    public string? SubFamily { get; set; }
    public decimal DefaultYield { get; set; } = 1m;
    public bool IsActive { get; set; } = true;
}

public sealed class SubFamilyCatalogItem
{
    public int SubFamilyId { get; set; }
    public string ProductFamily { get; set; } = string.Empty;
    public string SubFamilyName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class SmtFamilyLookupCatalogItem
{
    public string ProductFamily { get; set; } = string.Empty;
    public string? BusinessUnit { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class RackTypeCatalogItem
{
    public int RackTypeId { get; set; }
    public string RackCode { get; set; } = string.Empty;
    public string RackName { get; set; } = string.Empty;
    public decimal BaseUnitsPerRack { get; set; } = 1m;
    public bool IsActive { get; set; } = true;
}

public sealed class RackAccessoryCatalogItem
{
    public int RackAccessoryId { get; set; }
    public string AccessoryCode { get; set; } = string.Empty;
    public string AccessoryName { get; set; } = string.Empty;
    public decimal AdditionalUnitsPerRack { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ProcessRouteCatalogItem
{
    public int ProcessRouteId { get; set; }
    public int PartNumberId { get; set; }
    public int MachineId { get; set; }
    public string OperationName { get; set; } = string.Empty;
    public int SequenceNumber { get; set; } = 1;
    public decimal StandardTimeSeconds { get; set; } = 1m;
    public decimal? DefaultUtilizationFactor { get; set; }
    public decimal? DefaultYield { get; set; }
    public bool IsCapacityConstrained { get; set; } = true;
    public string PartNumber { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
}

public sealed class RackConfigurationCatalogItem
{
    public int RackConfigurationId { get; set; }
    public int RackTypeId { get; set; }
    public int? RackAccessoryId { get; set; }
    public int MachineId { get; set; }
    public int PartNumberId { get; set; }
    public decimal UnitsPerRack { get; set; } = 1m;
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public string RackName { get; set; } = string.Empty;
    public string? AccessoryName { get; set; }
    public string PartNumber { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
}

public sealed class PartCrossReferenceCatalogItem
{
    public int PartCrossReferenceId { get; set; }
    public int PartNumberId { get; set; }
    public string ReferenceType { get; set; } = string.Empty;
    public string ReferenceValue { get; set; } = string.Empty;
    public string PartNumber { get; set; } = string.Empty;
}

public sealed class OperationCatalogItem
{
    public int OperationId { get; set; }
    public string OperationCode { get; set; } = string.Empty;
    public string OperationName { get; set; } = string.Empty;
    public string? OperationFamily { get; set; }
    public int SequenceNumber { get; set; } = 1;
    public bool IsActive { get; set; } = true;
}

public sealed class TestTimeDefinitionCatalogItem
{
    public int TestTimeDefinitionId { get; set; }
    public int PartNumberId { get; set; }
    public int OperationId { get; set; }
    public string TestName { get; set; } = string.Empty;
    public decimal TestTimeMinutes { get; set; }
    public decimal HandlingMinutes { get; set; }
    public decimal QuantityCell { get; set; } = 1m;
    public decimal QuantityEquipment { get; set; } = 1m;
    public decimal? ManualStandardTimeSeconds { get; set; }
    public decimal CalculatedStandardTimeSeconds { get; set; }
    public bool IsActive { get; set; } = true;
    public string PartNumber { get; set; } = string.Empty;
    public string OperationCode { get; set; } = string.Empty;
}

public sealed class ResourceTypeCatalogItem
{
    public int ResourceTypeId { get; set; }
    public string ResourceCode { get; set; } = string.Empty;
    public string ResourceName { get; set; } = string.Empty;
    public string ResourceKind { get; set; } = "Rack";
    public bool IsActive { get; set; } = true;
}
