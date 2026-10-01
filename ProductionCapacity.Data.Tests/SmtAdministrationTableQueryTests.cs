namespace ProductionCapacity.Data.Tests;

public sealed class SmtAdministrationTableQueryTests
{
    [Fact]
    public void FiltersFamiliesBySearchTextAndSortsByPartCount()
    {
        var families = new[]
        {
            new SmtFamilyCatalogItem { ProductFamily = "BRONCO", BusinessUnit = "SRBU", PartNumberCount = 2, IsActive = true },
            new SmtFamilyCatalogItem { ProductFamily = "AZTECA", BusinessUnit = "ERBU", PartNumberCount = 8, IsActive = true },
            new SmtFamilyCatalogItem { ProductFamily = "CIELO", BusinessUnit = "ERBU", PartNumberCount = 1, IsActive = false }
        };

        var result = SmtAdministrationTableQuery.ApplyFamilies(
            families,
            "erbu",
            SmtFamilyCatalogSortColumn.PartNumberCount,
            descending: true);

        Assert.Collection(
            result,
            first => Assert.Equal("AZTECA", first.ProductFamily),
            second => Assert.Equal("CIELO", second.ProductFamily));
    }

    [Fact]
    public void FiltersFamilyRatesAndSortsByUnitsPerHour()
    {
        var rates = new[]
        {
            new SmtFamilyRateCatalogItem { ProductFamily = "AZTECA", OperationCode = "SMT", UnitsPerHour = 2.18m, ChangeoverCount = 4m, IsActive = true },
            new SmtFamilyRateCatalogItem { ProductFamily = "BRONCO", OperationCode = "SMT", UnitsPerHour = 3.25m, ChangeoverCount = 2m, IsActive = true },
            new SmtFamilyRateCatalogItem { ProductFamily = "CIELO", OperationCode = "PACK", UnitsPerHour = 1.15m, ChangeoverCount = 1m, IsActive = true }
        };

        var result = SmtAdministrationTableQuery.ApplyFamilyRates(
            rates,
            "smt",
            SmtFamilyRateCatalogSortColumn.UnitsPerHour,
            descending: false);

        Assert.Collection(
            result,
            first => Assert.Equal("AZTECA", first.ProductFamily),
            second => Assert.Equal("BRONCO", second.ProductFamily));
    }

    [Fact]
    public void FiltersLineRatesAndSortsByLineNumber()
    {
        var rates = new[]
        {
            new SmtLineFamilyRateCatalogItem { ProductFamily = "AZTECA", OperationCode = "SMT", LineCode = "SMT 2", LineNumber = 2, UnitsPerHour = 2.18m, IsActive = true },
            new SmtLineFamilyRateCatalogItem { ProductFamily = "BRONCO", OperationCode = "SMT", LineCode = "SMT 1", LineNumber = 1, UnitsPerHour = 3.25m, IsActive = true },
            new SmtLineFamilyRateCatalogItem { ProductFamily = "CIELO", OperationCode = "PACK", LineCode = "PACK 1", LineNumber = 1, UnitsPerHour = 1.15m, IsActive = true }
        };

        var result = SmtAdministrationTableQuery.ApplyLineRates(
            rates,
            "smt",
            SmtLineFamilyRateCatalogSortColumn.LineNumber,
            descending: false);

        Assert.Collection(
            result,
            first => Assert.Equal("SMT 1", first.LineCode),
            second => Assert.Equal("SMT 2", second.LineCode));
    }

    [Fact]
    public void FiltersLineAssignmentsAndSortsByDistribution()
    {
        var assignments = new[]
        {
            new SmtLineAssignmentCatalogItem { ScenarioName = "Base", WeekName = "M1", OperationCode = "SMT", ProductFamily = "AZTECA", LineCode = "SMT 1", LineNumber = 1, LineDistributionPercent = 60m, IsActive = true },
            new SmtLineAssignmentCatalogItem { ScenarioName = "Base", WeekName = "M1", OperationCode = "SMT", ProductFamily = "BRONCO", LineCode = "SMT 2", LineNumber = 2, LineDistributionPercent = 40m, IsActive = true },
            new SmtLineAssignmentCatalogItem { ScenarioName = "Base", WeekName = "M2", OperationCode = "PACK", ProductFamily = "CIELO", LineCode = "PACK 1", LineNumber = 1, LineDistributionPercent = 100m, IsActive = false }
        };

        var result = SmtAdministrationTableQuery.ApplyLineAssignments(
            assignments,
            "smt",
            SmtLineAssignmentCatalogSortColumn.LineDistributionPercent,
            descending: false);

        Assert.Collection(
            result,
            first => Assert.Equal("BRONCO", first.ProductFamily),
            second => Assert.Equal("AZTECA", second.ProductFamily));
    }

    [Fact]
    public void FiltersOperationCapacitiesAndSortsByOvertime()
    {
        var capacities = new[]
        {
            new SmtOperationCapacityCatalogItem { ScenarioName = "Base", WeekName = "M1", OperationCode = "SMT", WorkDays = 5m, HoursPerShift = 7.5m, ShiftsPerDay = 3, EquipmentQuantity = 6m, OvertimeHours = 4m },
            new SmtOperationCapacityCatalogItem { ScenarioName = "Base", WeekName = "M2", OperationCode = "SMT", WorkDays = 4m, HoursPerShift = 7.5m, ShiftsPerDay = 3, EquipmentQuantity = 6m, OvertimeHours = 1m },
            new SmtOperationCapacityCatalogItem { ScenarioName = "Base", WeekName = "M3", OperationCode = "PACK", WorkDays = 5m, HoursPerShift = 7.5m, ShiftsPerDay = 2, EquipmentQuantity = 1m, OvertimeHours = 8m }
        };

        var result = SmtAdministrationTableQuery.ApplyOperationCapacities(
            capacities,
            "smt",
            SmtOperationCapacityCatalogSortColumn.OvertimeHours,
            descending: true);

        Assert.Collection(
            result,
            first => Assert.Equal("M1", first.WeekName),
            second => Assert.Equal("M2", second.WeekName));
    }

    [Fact]
    public void FiltersCapacityTargetsAndAuditLogs()
    {
        var targets = new[]
        {
            new SmtCapacityTargetCatalogItem { ScenarioName = "Base", WeekName = "M1", OperationCode = "SMT", ProductFamily = "AZTECA", LineCode = "SMT 1", ExpectedUtilizationPercent = 60m, ExpectedDeltaCapacityUnits = 100m, SourceReference = "Capacity SMT!A1" },
            new SmtCapacityTargetCatalogItem { ScenarioName = "Base", WeekName = "M2", OperationCode = "SMT", ProductFamily = "BRONCO", LineCode = "SMT 2", ExpectedUtilizationPercent = 80m, ExpectedDeltaCapacityUnits = 25m, SourceReference = "Capacity SMT!A2" },
            new SmtCapacityTargetCatalogItem { ScenarioName = "Base", WeekName = "M3", OperationCode = "PACK", ProductFamily = "CIELO", LineCode = "PACK 1", ExpectedUtilizationPercent = 20m, ExpectedDeltaCapacityUnits = 300m, SourceReference = "Capacity PACK!A3" }
        };
        var logs = new[]
        {
            new SmtAuditLogItem { EntityType = "UPH Familia SMT", EntityKey = "AZTECA|SMT", ActionType = "Actualizacion", ChangedBy = "Admin SMT", CreatedAtUtc = new DateTime(2026, 9, 14, 8, 0, 0, DateTimeKind.Utc) },
            new SmtAuditLogItem { EntityType = "Calendario SMT", EntityKey = "SMT|M2", ActionType = "Creacion", ChangedBy = "Importacion", CreatedAtUtc = new DateTime(2026, 9, 15, 8, 0, 0, DateTimeKind.Utc) }
        };

        var targetResult = SmtAdministrationTableQuery.ApplyCapacityTargets(
            targets,
            "smt",
            SmtCapacityTargetCatalogSortColumn.ExpectedUtilizationPercent,
            descending: true);
        var logResult = SmtAdministrationTableQuery.ApplyAuditLogs(
            logs,
            "azteca",
            SmtAuditLogSortColumn.CreatedAtUtc,
            descending: true);

        Assert.Collection(
            targetResult,
            first => Assert.Equal("BRONCO", first.ProductFamily),
            second => Assert.Equal("AZTECA", second.ProductFamily));
        var log = Assert.Single(logResult);
        Assert.Equal("UPH Familia SMT", log.EntityType);
    }
}
