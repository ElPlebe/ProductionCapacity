namespace ProductionCapacity.Data.Entities;

public sealed class Scenario
{
    public int ScenarioId { get; set; }
    public string ScenarioName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsBaseline { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class ProductionWeekEntity
{
    public int ProductionWeekId { get; set; }
    public int ScenarioId { get; set; }
    public string WeekName { get; set; } = string.Empty;
    public DateOnly? WeekStartDate { get; set; }
    public decimal WorkDays { get; set; }
    public decimal HoursPerShift { get; set; }
    public int ShiftsPerDay { get; set; }
    public decimal HolidayDays { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal PlannedDowntimeHours { get; set; }
}

public sealed class MachineEntity
{
    public int MachineId { get; set; }
    public string MachineCode { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public string? MachineType { get; set; }
    public decimal DefaultUtilizationFactor { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class PartNumberEntity
{
    public int PartNumberId { get; set; }
    public string PartNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? BusinessUnit { get; set; }
    public string? ProductFamily { get; set; }
    public string? SubFamily { get; set; }
    public decimal DefaultYield { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class SubFamilyEntity
{
    public int SubFamilyId { get; set; }
    public string ProductFamily { get; set; } = string.Empty;
    public string SubFamilyName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class PartCrossReference
{
    public int PartCrossReferenceId { get; set; }
    public int PartNumberId { get; set; }
    public string ReferenceType { get; set; } = string.Empty;
    public string ReferenceValue { get; set; } = string.Empty;
}

public sealed class ProcessRoute
{
    public int ProcessRouteId { get; set; }
    public int PartNumberId { get; set; }
    public int MachineId { get; set; }
    public string OperationName { get; set; } = string.Empty;
    public int SequenceNumber { get; set; }
    public decimal StandardTimeSeconds { get; set; }
    public decimal? DefaultUtilizationFactor { get; set; }
    public decimal? DefaultYield { get; set; }
    public bool IsCapacityConstrained { get; set; } = true;
}

public sealed class PlanningOperation
{
    public int OperationId { get; set; }
    public string OperationCode { get; set; } = string.Empty;
    public string OperationName { get; set; } = string.Empty;
    public string? OperationFamily { get; set; }
    public int SequenceNumber { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class OperationWeeklyCapacityEntity
{
    public int OperationWeeklyCapacityId { get; set; }
    public int ScenarioId { get; set; }
    public int ProductionWeekId { get; set; }
    public int OperationId { get; set; }
    public decimal WorkDays { get; set; }
    public decimal HoursPerShift { get; set; }
    public int ShiftsPerDay { get; set; }
    public decimal EquipmentQuantity { get; set; }
    public decimal HolidayDays { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal PlannedDowntimeHours { get; set; }
}

public sealed class OperationRateEntity
{
    public int OperationRateId { get; set; }
    public int PartNumberId { get; set; }
    public int OperationId { get; set; }
    public decimal StandardTimeSeconds { get; set; }
    public decimal YieldFactor { get; set; } = 1m;
    public decimal UtilizationFactor { get; set; } = 1m;
    public decimal EfficiencyFactor { get; set; } = 1m;
    public decimal? MaxCapacityUnits { get; set; }
    public decimal? FixedHeadcount { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class OperationCapacityResultEntity
{
    public long OperationCapacityResultId { get; set; }
    public long? CalculationRunId { get; set; }
    public int ScenarioId { get; set; }
    public int ProductionWeekId { get; set; }
    public int PartNumberId { get; set; }
    public int OperationId { get; set; }
    public decimal DemandUnits { get; set; }
    public decimal AvailableHours { get; set; }
    public decimal EffectiveUnitsPerHour { get; set; }
    public decimal LoadHours { get; set; }
    public decimal TimeCapacityUnits { get; set; }
    public decimal RemainingHours { get; set; }
    public decimal LoadPercent { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class TestTimeDefinitionEntity
{
    public int TestTimeDefinitionId { get; set; }
    public int PartNumberId { get; set; }
    public int OperationId { get; set; }
    public string TestName { get; set; } = string.Empty;
    public decimal TestTimeMinutes { get; set; }
    public decimal HandlingMinutes { get; set; }
    public decimal QuantityCell { get; set; }
    public decimal QuantityEquipment { get; set; }
    public decimal? ManualStandardTimeSeconds { get; set; }
    public decimal CalculatedStandardTimeSeconds { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ResourceTypeEntity
{
    public int ResourceTypeId { get; set; }
    public string ResourceCode { get; set; } = string.Empty;
    public string ResourceName { get; set; } = string.Empty;
    public string ResourceKind { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class ResourceConfigurationEntity
{
    public int ResourceConfigurationId { get; set; }
    public int ResourceTypeId { get; set; }
    public int OperationId { get; set; }
    public int? PartNumberId { get; set; }
    public string? ProductFamily { get; set; }
    public decimal UnitsPerResource { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
}

public sealed class ResourceInventoryEntity
{
    public int ResourceInventoryId { get; set; }
    public int ScenarioId { get; set; }
    public int ProductionWeekId { get; set; }
    public int ResourceTypeId { get; set; }
    public int AvailableResourceCount { get; set; }
    public string? Notes { get; set; }
}

public sealed class SmtFamilyVolumeEntity
{
    public int SmtFamilyVolumeId { get; set; }
    public int ScenarioId { get; set; }
    public int ProductionWeekId { get; set; }
    public string ProductFamily { get; set; } = string.Empty;
    public string? BusinessUnit { get; set; }
    public string? MonthName { get; set; }
    public int MonthSequence { get; set; }
    public decimal MonthlyMps { get; set; }
    public decimal WeeklyMps { get; set; }
}

public sealed class SmtFamilyEntity
{
    public int SmtFamilyId { get; set; }
    public string ProductFamily { get; set; } = string.Empty;
    public string? BusinessUnit { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class SmtFamilyRateEntity
{
    public int SmtFamilyRateId { get; set; }
    public int OperationId { get; set; }
    public string ProductFamily { get; set; } = string.Empty;
    public decimal UnitsPerHour { get; set; }
    public decimal ChangeoverCount { get; set; }
    public string? SourcePartNumber { get; set; }
    public decimal? BottleneckUnitsPerHour { get; set; }
    public decimal? RouterUnitsPerHour { get; set; }
    public decimal? WaveUnitsPerHour { get; set; }
    public decimal? PressfitUnitsPerHour { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class SmtAuditLogEntity
{
    public long SmtAuditLogId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityKey { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string? FieldName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class SmtLineFamilyRateEntity
{
    public int SmtLineFamilyRateId { get; set; }
    public int OperationId { get; set; }
    public string ProductFamily { get; set; } = string.Empty;
    public string LineCode { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public decimal UnitsPerHour { get; set; }
    public string? SourcePartNumber { get; set; }
    public decimal? BottleneckUnitsPerHour { get; set; }
    public decimal? RouterUnitsPerHour { get; set; }
    public decimal? WaveUnitsPerHour { get; set; }
    public decimal? PressfitUnitsPerHour { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class SmtLineAssignmentEntity
{
    public int SmtLineAssignmentId { get; set; }
    public int ScenarioId { get; set; }
    public int ProductionWeekId { get; set; }
    public int OperationId { get; set; }
    public string ProductFamily { get; set; } = string.Empty;
    public string LineCode { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public decimal LineDistribution { get; set; }
    public string? MonthName { get; set; }
    public int MonthSequence { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class SmtCapacityTargetEntity
{
    public int SmtCapacityTargetId { get; set; }
    public int ScenarioId { get; set; }
    public int ProductionWeekId { get; set; }
    public int OperationId { get; set; }
    public string ProductFamily { get; set; } = string.Empty;
    public string LineCode { get; set; } = string.Empty;
    public decimal ExpectedAssignedDemandUnits { get; set; }
    public decimal ExpectedMaxCapacityUnits { get; set; }
    public decimal ExpectedUtilizationPercent { get; set; }
    public decimal ExpectedDeltaCapacityUnits { get; set; }
    public string? SourceReference { get; set; }
    public string? Notes { get; set; }
}

public sealed class WeeklyDemandEntity
{
    public int WeeklyDemandId { get; set; }
    public int ScenarioId { get; set; }
    public int ProductionWeekId { get; set; }
    public int PartNumberId { get; set; }
    public decimal DemandUnits { get; set; }
}

public sealed class ScenarioPartOverride
{
    public int ScenarioPartOverrideId { get; set; }
    public int ScenarioId { get; set; }
    public int PartNumberId { get; set; }
    public decimal? YieldOverride { get; set; }
    public string? Notes { get; set; }
}

public sealed class ScenarioMachineOverride
{
    public int ScenarioMachineOverrideId { get; set; }
    public int ScenarioId { get; set; }
    public int MachineId { get; set; }
    public decimal? UtilizationFactorOverride { get; set; }
    public string? Notes { get; set; }
}

public sealed class ScenarioMachinePartOverride
{
    public int ScenarioMachinePartOverrideId { get; set; }
    public int ScenarioId { get; set; }
    public int MachineId { get; set; }
    public int PartNumberId { get; set; }
    public decimal? UtilizationFactorOverride { get; set; }
    public decimal? YieldOverride { get; set; }
    public decimal? StandardTimeSecondsOverride { get; set; }
    public string? Notes { get; set; }
}

public sealed class RackType
{
    public int RackTypeId { get; set; }
    public string RackCode { get; set; } = string.Empty;
    public string RackName { get; set; } = string.Empty;
    public decimal BaseUnitsPerRack { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class RackAccessory
{
    public int RackAccessoryId { get; set; }
    public string AccessoryCode { get; set; } = string.Empty;
    public string AccessoryName { get; set; } = string.Empty;
    public decimal AdditionalUnitsPerRack { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class RackConfiguration
{
    public int RackConfigurationId { get; set; }
    public int RackTypeId { get; set; }
    public int? RackAccessoryId { get; set; }
    public int MachineId { get; set; }
    public int PartNumberId { get; set; }
    public decimal UnitsPerRack { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ScenarioRackInventory
{
    public int ScenarioRackInventoryId { get; set; }
    public int ScenarioId { get; set; }
    public int ProductionWeekId { get; set; }
    public int MachineId { get; set; }
    public int RackTypeId { get; set; }
    public int AvailableRackCount { get; set; }
    public string? Notes { get; set; }
}
