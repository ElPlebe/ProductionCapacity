namespace ProductionCapacity.Data.ExcelImport;

public sealed record ExcelCapacityImportPackage(
    IReadOnlyList<ExcelScenarioImportRow> Scenarios,
    IReadOnlyList<ExcelProductionWeekImportRow> ProductionWeeks,
    IReadOnlyList<ExcelPartNumberImportRow> PartNumbers,
    IReadOnlyList<ExcelWeeklyDemandImportRow> WeeklyDemands,
    IReadOnlyList<ExcelOperationImportRow> Operations,
    IReadOnlyList<ExcelOperationRateImportRow> OperationRates,
    IReadOnlyList<ExcelOperationCapacityImportRow> OperationCapacities,
    IReadOnlyList<ExcelCrossReferenceImportRow> CrossReferences,
    IReadOnlyList<ExcelTestTimeImportRow> TestTimes,
    IReadOnlyList<ExcelResourceTypeImportRow> ResourceTypes,
    IReadOnlyList<ExcelResourceConfigurationImportRow> ResourceConfigurations,
    IReadOnlyList<ExcelResourceInventoryImportRow> ResourceInventories,
    IReadOnlyList<ExcelSmtFamilyVolumeImportRow> SmtFamilyVolumes,
    IReadOnlyList<ExcelSmtFamilyRateImportRow> SmtFamilyRates,
    IReadOnlyList<ExcelSmtLineFamilyRateImportRow> SmtLineFamilyRates,
    IReadOnlyList<ExcelSmtLineAssignmentImportRow> SmtLineAssignments,
    IReadOnlyList<ExcelSmtCapacityTargetImportRow> SmtCapacityTargets,
    IReadOnlyList<string> SourceWarnings);

public sealed record ExcelScenarioImportRow(
    string ScenarioName,
    string? Description,
    bool IsBaseline);

public sealed record ExcelProductionWeekImportRow(
    string ScenarioName,
    string WeekName,
    DateOnly? WeekStartDate,
    decimal WorkDays,
    decimal HoursPerShift,
    int ShiftsPerDay,
    decimal HolidayDays,
    decimal OvertimeHours,
    decimal PlannedDowntimeHours);

public sealed record ExcelPartNumberImportRow(
    string PartNumber,
    string Description,
    string? BusinessUnit,
    string? ProductFamily,
    decimal DefaultYield,
    string? SubFamily = null);

public sealed record ExcelWeeklyDemandImportRow(
    string ScenarioName,
    string WeekName,
    string? PartNumber,
    string? ProductFamily,
    decimal DemandUnits);

public sealed record ExcelOperationImportRow(
    string OperationCode,
    string OperationName,
    string? OperationFamily,
    int SequenceNumber);

public sealed record ExcelOperationRateImportRow(
    string? PartNumber,
    string? ProductFamily,
    string OperationCode,
    decimal StandardTimeSeconds,
    decimal YieldFactor,
    decimal UtilizationFactor,
    decimal EfficiencyFactor,
    decimal? MaxCapacityUnits,
    decimal? FixedHeadcount);

public sealed record ExcelOperationCapacityImportRow(
    string ScenarioName,
    string WeekName,
    string OperationCode,
    decimal WorkDays,
    decimal HoursPerShift,
    int ShiftsPerDay,
    decimal EquipmentQuantity,
    decimal HolidayDays,
    decimal OvertimeHours,
    decimal PlannedDowntimeHours);

public sealed record ExcelCrossReferenceImportRow(
    string PartNumber,
    string ReferenceType,
    string ReferenceValue);

public sealed record ExcelTestTimeImportRow(
    string? PartNumber,
    string? ProductFamily,
    string OperationCode,
    string TestName,
    decimal TestTimeMinutes,
    decimal HandlingMinutes,
    decimal QuantityCell,
    decimal QuantityEquipment,
    decimal? ManualStandardTimeSeconds);

public sealed record ExcelResourceTypeImportRow(
    string ResourceCode,
    string ResourceName,
    string ResourceKind);

public sealed record ExcelResourceConfigurationImportRow(
    string ResourceCode,
    string OperationCode,
    string? PartNumber,
    string? ProductFamily,
    decimal UnitsPerResource,
    string? Notes);

public sealed record ExcelResourceInventoryImportRow(
    string ScenarioName,
    string WeekName,
    string ResourceCode,
    int AvailableResourceCount,
    string? Notes);

public sealed record ExcelSmtFamilyVolumeImportRow(
    string ScenarioName,
    string WeekName,
    string? BusinessUnit,
    string ProductFamily,
    string? MonthName,
    int MonthSequence,
    decimal MonthlyMps,
    decimal WeeklyMps);

public sealed record ExcelSmtFamilyRateImportRow(
    string ProductFamily,
    string OperationCode,
    decimal UnitsPerHour,
    decimal ChangeoverCount,
    string? SourcePartNumber,
    decimal? BottleneckUnitsPerHour,
    decimal? RouterUnitsPerHour,
    decimal? WaveUnitsPerHour,
    decimal? PressfitUnitsPerHour);

public sealed record ExcelSmtLineFamilyRateImportRow(
    string ProductFamily,
    string OperationCode,
    string LineCode,
    int LineNumber,
    decimal UnitsPerHour,
    string? SourcePartNumber,
    decimal? BottleneckUnitsPerHour,
    decimal? RouterUnitsPerHour,
    decimal? WaveUnitsPerHour,
    decimal? PressfitUnitsPerHour);

public sealed record ExcelSmtLineAssignmentImportRow(
    string ScenarioName,
    string WeekName,
    string ProductFamily,
    string OperationCode,
    string LineCode,
    int LineNumber,
    decimal LineDistribution,
    string? MonthName,
    int MonthSequence);

public sealed record ExcelSmtCapacityTargetImportRow(
    string ScenarioName,
    string WeekName,
    string ProductFamily,
    string OperationCode,
    string LineCode,
    decimal ExpectedAssignedDemandUnits,
    decimal ExpectedMaxCapacityUnits,
    decimal ExpectedUtilizationPercent,
    decimal ExpectedDeltaCapacityUnits,
    string? SourceReference,
    string? Notes);

public sealed record ExcelCapacityImportPreview(
    int ScenarioCount,
    int ProductionWeekCount,
    int PartNumberCount,
    int WeeklyDemandCount,
    int OperationCount,
    int OperationRateCount,
    int OperationCapacityCount,
    int CrossReferenceCount,
    int TestTimeCount,
    int ResourceTypeCount,
    int ResourceConfigurationCount,
    int ResourceInventoryCount,
    int SmtFamilyVolumeCount,
    int SmtFamilyRateCount,
    int SmtLineFamilyRateCount,
    int SmtLineAssignmentCount,
    int SmtCapacityTargetCount,
    IReadOnlyList<string> Warnings,
    ExcelSmtImportImpact SmtImpact);

public sealed record ExcelSmtImportImpact(
    IReadOnlyList<ExcelSmtFamilyImpactItem> NewFamilies,
    IReadOnlyList<ExcelSmtFamilyImpactItem> ExistingFamilies,
    IReadOnlyList<ExcelSmtFamilyRateReplacement> FamilyRateReplacements,
    IReadOnlyList<ExcelSmtImportIssue> IncompleteItems)
{
    public static ExcelSmtImportImpact Empty { get; } = new([], [], [], []);
}

public sealed record ExcelSmtFamilyImpactItem(
    string ProductFamily,
    string? BusinessUnit);

public sealed record ExcelSmtFamilyRateReplacement(
    string ProductFamily,
    string OperationCode,
    decimal CurrentUnitsPerHour,
    decimal ImportedUnitsPerHour);

public sealed record ExcelSmtImportIssue(
    string ItemType,
    string ProductFamily,
    string Message);
