namespace ProductionCapacity.Data;

public enum SmtFamilyCatalogSortColumn
{
    ProductFamily,
    BusinessUnit,
    PartNumberCount,
    IsActive
}

public enum SmtFamilyRateCatalogSortColumn
{
    ProductFamily,
    OperationCode,
    UnitsPerHour,
    ChangeoverCount,
    SourcePartNumber,
    IsActive
}

public enum SmtLineFamilyRateCatalogSortColumn
{
    ProductFamily,
    OperationCode,
    LineCode,
    LineNumber,
    UnitsPerHour,
    SourcePartNumber,
    IsActive
}

public enum SmtLineAssignmentCatalogSortColumn
{
    ScenarioName,
    WeekName,
    OperationCode,
    ProductFamily,
    LineCode,
    LineNumber,
    LineDistributionPercent,
    IsActive
}

public enum SmtOperationCapacityCatalogSortColumn
{
    ScenarioName,
    WeekName,
    OperationCode,
    WorkDays,
    HoursPerShift,
    ShiftsPerDay,
    EquipmentQuantity,
    HolidayDays,
    OvertimeHours,
    PlannedDowntimeHours
}

public enum SmtCapacityTargetCatalogSortColumn
{
    ScenarioName,
    WeekName,
    OperationCode,
    ProductFamily,
    LineCode,
    ExpectedAssignedDemandUnits,
    ExpectedMaxCapacityUnits,
    ExpectedUtilizationPercent,
    ExpectedDeltaCapacityUnits,
    SourceReference
}

public enum SmtAuditLogSortColumn
{
    CreatedAtUtc,
    ChangedBy,
    ActionType,
    EntityType,
    EntityKey
}

public static class SmtAdministrationTableQuery
{
    public static IReadOnlyList<SmtFamilyCatalogItem> ApplyFamilies(
        IEnumerable<SmtFamilyCatalogItem> rows,
        string? searchText,
        SmtFamilyCatalogSortColumn sortColumn,
        bool descending)
    {
        var filtered = rows.Where(row => Matches(searchText, row.ProductFamily, row.BusinessUnit, row.Notes, FormatActive(row.IsActive)));
        return SortFamilies(filtered, sortColumn, descending).ToList();
    }

    public static IReadOnlyList<SmtFamilyRateCatalogItem> ApplyFamilyRates(
        IEnumerable<SmtFamilyRateCatalogItem> rows,
        string? searchText,
        SmtFamilyRateCatalogSortColumn sortColumn,
        bool descending)
    {
        var filtered = rows.Where(row => Matches(
            searchText,
            row.ProductFamily,
            row.OperationCode,
            row.SourcePartNumber,
            FormatActive(row.IsActive)));
        return SortFamilyRates(filtered, sortColumn, descending).ToList();
    }

    public static IReadOnlyList<SmtLineFamilyRateCatalogItem> ApplyLineRates(
        IEnumerable<SmtLineFamilyRateCatalogItem> rows,
        string? searchText,
        SmtLineFamilyRateCatalogSortColumn sortColumn,
        bool descending)
    {
        var filtered = rows.Where(row => Matches(
            searchText,
            row.ProductFamily,
            row.OperationCode,
            row.LineCode,
            row.SourcePartNumber,
            FormatActive(row.IsActive)));
        return SortLineRates(filtered, sortColumn, descending).ToList();
    }

    public static IReadOnlyList<SmtLineAssignmentCatalogItem> ApplyLineAssignments(
        IEnumerable<SmtLineAssignmentCatalogItem> rows,
        string? searchText,
        SmtLineAssignmentCatalogSortColumn sortColumn,
        bool descending)
    {
        var filtered = rows.Where(row => Matches(
            searchText,
            row.ScenarioName,
            row.WeekName,
            row.OperationCode,
            row.ProductFamily,
            row.LineCode,
            FormatActive(row.IsActive)));
        return SortLineAssignments(filtered, sortColumn, descending).ToList();
    }

    public static IReadOnlyList<SmtOperationCapacityCatalogItem> ApplyOperationCapacities(
        IEnumerable<SmtOperationCapacityCatalogItem> rows,
        string? searchText,
        SmtOperationCapacityCatalogSortColumn sortColumn,
        bool descending)
    {
        var filtered = rows.Where(row => Matches(
            searchText,
            row.ScenarioName,
            row.WeekName,
            row.OperationCode));
        return SortOperationCapacities(filtered, sortColumn, descending).ToList();
    }

    public static IReadOnlyList<SmtCapacityTargetCatalogItem> ApplyCapacityTargets(
        IEnumerable<SmtCapacityTargetCatalogItem> rows,
        string? searchText,
        SmtCapacityTargetCatalogSortColumn sortColumn,
        bool descending)
    {
        var filtered = rows.Where(row => Matches(
            searchText,
            row.ScenarioName,
            row.WeekName,
            row.OperationCode,
            row.ProductFamily,
            row.LineCode,
            row.SourceReference,
            row.Notes));
        return SortCapacityTargets(filtered, sortColumn, descending).ToList();
    }

    public static IReadOnlyList<SmtAuditLogItem> ApplyAuditLogs(
        IEnumerable<SmtAuditLogItem> rows,
        string? searchText,
        SmtAuditLogSortColumn sortColumn,
        bool descending)
    {
        var filtered = rows.Where(row => Matches(
            searchText,
            row.EntityType,
            row.EntityKey,
            row.ActionType,
            row.ChangedBy,
            row.OldValue,
            row.NewValue,
            row.Notes));
        return SortAuditLogs(filtered, sortColumn, descending).ToList();
    }

    private static IOrderedEnumerable<SmtFamilyCatalogItem> SortFamilies(
        IEnumerable<SmtFamilyCatalogItem> rows,
        SmtFamilyCatalogSortColumn sortColumn,
        bool descending) =>
        sortColumn switch
        {
            SmtFamilyCatalogSortColumn.BusinessUnit => SortBy(rows, row => row.BusinessUnit ?? string.Empty, descending),
            SmtFamilyCatalogSortColumn.PartNumberCount => SortBy(rows, row => row.PartNumberCount, descending),
            SmtFamilyCatalogSortColumn.IsActive => SortBy(rows, row => row.IsActive, descending),
            _ => SortBy(rows, row => row.ProductFamily, descending)
        };

    private static IOrderedEnumerable<SmtFamilyRateCatalogItem> SortFamilyRates(
        IEnumerable<SmtFamilyRateCatalogItem> rows,
        SmtFamilyRateCatalogSortColumn sortColumn,
        bool descending) =>
        sortColumn switch
        {
            SmtFamilyRateCatalogSortColumn.OperationCode => SortBy(rows, row => row.OperationCode, descending),
            SmtFamilyRateCatalogSortColumn.UnitsPerHour => SortBy(rows, row => row.UnitsPerHour, descending),
            SmtFamilyRateCatalogSortColumn.ChangeoverCount => SortBy(rows, row => row.ChangeoverCount, descending),
            SmtFamilyRateCatalogSortColumn.SourcePartNumber => SortBy(rows, row => row.SourcePartNumber ?? string.Empty, descending),
            SmtFamilyRateCatalogSortColumn.IsActive => SortBy(rows, row => row.IsActive, descending),
            _ => SortBy(rows, row => row.ProductFamily, descending)
        };

    private static IOrderedEnumerable<SmtLineFamilyRateCatalogItem> SortLineRates(
        IEnumerable<SmtLineFamilyRateCatalogItem> rows,
        SmtLineFamilyRateCatalogSortColumn sortColumn,
        bool descending) =>
        sortColumn switch
        {
            SmtLineFamilyRateCatalogSortColumn.OperationCode => SortBy(rows, row => row.OperationCode, descending),
            SmtLineFamilyRateCatalogSortColumn.LineCode => SortBy(rows, row => row.LineCode, descending),
            SmtLineFamilyRateCatalogSortColumn.LineNumber => SortBy(rows, row => row.LineNumber, descending),
            SmtLineFamilyRateCatalogSortColumn.UnitsPerHour => SortBy(rows, row => row.UnitsPerHour, descending),
            SmtLineFamilyRateCatalogSortColumn.SourcePartNumber => SortBy(rows, row => row.SourcePartNumber ?? string.Empty, descending),
            SmtLineFamilyRateCatalogSortColumn.IsActive => SortBy(rows, row => row.IsActive, descending),
            _ => SortBy(rows, row => row.ProductFamily, descending)
        };

    private static IOrderedEnumerable<SmtLineAssignmentCatalogItem> SortLineAssignments(
        IEnumerable<SmtLineAssignmentCatalogItem> rows,
        SmtLineAssignmentCatalogSortColumn sortColumn,
        bool descending) =>
        sortColumn switch
        {
            SmtLineAssignmentCatalogSortColumn.ScenarioName => SortBy(rows, row => row.ScenarioName, descending),
            SmtLineAssignmentCatalogSortColumn.WeekName => SortBy(rows, row => row.WeekName, descending),
            SmtLineAssignmentCatalogSortColumn.OperationCode => SortBy(rows, row => row.OperationCode, descending),
            SmtLineAssignmentCatalogSortColumn.LineCode => SortBy(rows, row => row.LineCode, descending),
            SmtLineAssignmentCatalogSortColumn.LineNumber => SortBy(rows, row => row.LineNumber, descending),
            SmtLineAssignmentCatalogSortColumn.LineDistributionPercent => SortBy(rows, row => row.LineDistributionPercent, descending),
            SmtLineAssignmentCatalogSortColumn.IsActive => SortBy(rows, row => row.IsActive, descending),
            _ => SortBy(rows, row => row.ProductFamily, descending)
        };

    private static IOrderedEnumerable<SmtOperationCapacityCatalogItem> SortOperationCapacities(
        IEnumerable<SmtOperationCapacityCatalogItem> rows,
        SmtOperationCapacityCatalogSortColumn sortColumn,
        bool descending) =>
        sortColumn switch
        {
            SmtOperationCapacityCatalogSortColumn.ScenarioName => SortBy(rows, row => row.ScenarioName, descending),
            SmtOperationCapacityCatalogSortColumn.OperationCode => SortBy(rows, row => row.OperationCode, descending),
            SmtOperationCapacityCatalogSortColumn.WorkDays => SortBy(rows, row => row.WorkDays, descending),
            SmtOperationCapacityCatalogSortColumn.HoursPerShift => SortBy(rows, row => row.HoursPerShift, descending),
            SmtOperationCapacityCatalogSortColumn.ShiftsPerDay => SortBy(rows, row => row.ShiftsPerDay, descending),
            SmtOperationCapacityCatalogSortColumn.EquipmentQuantity => SortBy(rows, row => row.EquipmentQuantity, descending),
            SmtOperationCapacityCatalogSortColumn.HolidayDays => SortBy(rows, row => row.HolidayDays, descending),
            SmtOperationCapacityCatalogSortColumn.OvertimeHours => SortBy(rows, row => row.OvertimeHours, descending),
            SmtOperationCapacityCatalogSortColumn.PlannedDowntimeHours => SortBy(rows, row => row.PlannedDowntimeHours, descending),
            _ => SortBy(rows, row => row.WeekName, descending)
        };

    private static IOrderedEnumerable<SmtCapacityTargetCatalogItem> SortCapacityTargets(
        IEnumerable<SmtCapacityTargetCatalogItem> rows,
        SmtCapacityTargetCatalogSortColumn sortColumn,
        bool descending) =>
        sortColumn switch
        {
            SmtCapacityTargetCatalogSortColumn.ScenarioName => SortBy(rows, row => row.ScenarioName, descending),
            SmtCapacityTargetCatalogSortColumn.WeekName => SortBy(rows, row => row.WeekName, descending),
            SmtCapacityTargetCatalogSortColumn.OperationCode => SortBy(rows, row => row.OperationCode, descending),
            SmtCapacityTargetCatalogSortColumn.LineCode => SortBy(rows, row => row.LineCode, descending),
            SmtCapacityTargetCatalogSortColumn.ExpectedAssignedDemandUnits => SortBy(rows, row => row.ExpectedAssignedDemandUnits, descending),
            SmtCapacityTargetCatalogSortColumn.ExpectedMaxCapacityUnits => SortBy(rows, row => row.ExpectedMaxCapacityUnits, descending),
            SmtCapacityTargetCatalogSortColumn.ExpectedUtilizationPercent => SortBy(rows, row => row.ExpectedUtilizationPercent, descending),
            SmtCapacityTargetCatalogSortColumn.ExpectedDeltaCapacityUnits => SortBy(rows, row => row.ExpectedDeltaCapacityUnits, descending),
            SmtCapacityTargetCatalogSortColumn.SourceReference => SortBy(rows, row => row.SourceReference ?? string.Empty, descending),
            _ => SortBy(rows, row => row.ProductFamily, descending)
        };

    private static IOrderedEnumerable<SmtAuditLogItem> SortAuditLogs(
        IEnumerable<SmtAuditLogItem> rows,
        SmtAuditLogSortColumn sortColumn,
        bool descending) =>
        sortColumn switch
        {
            SmtAuditLogSortColumn.ChangedBy => SortBy(rows, row => row.ChangedBy, descending),
            SmtAuditLogSortColumn.ActionType => SortBy(rows, row => row.ActionType, descending),
            SmtAuditLogSortColumn.EntityType => SortBy(rows, row => row.EntityType, descending),
            SmtAuditLogSortColumn.EntityKey => SortBy(rows, row => row.EntityKey, descending),
            _ => SortBy(rows, row => row.CreatedAtUtc, descending)
        };

    private static IOrderedEnumerable<T> SortBy<T, TKey>(
        IEnumerable<T> rows,
        Func<T, TKey> selector,
        bool descending) =>
        descending
            ? rows.OrderByDescending(selector).ThenBy(row => 0)
            : rows.OrderBy(selector).ThenBy(row => 0);

    private static bool Matches(string? searchText, params string?[] values)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return true;
        }

        var text = searchText.Trim();
        return values.Any(value => value?.Contains(text, StringComparison.OrdinalIgnoreCase) == true);
    }

    private static string FormatActive(bool isActive) => isActive ? "Activo" : "Inactivo";
}
