namespace ProductionCapacity.Data;

public sealed record PlanningMatrix(
    string ScenarioName,
    string WeekName,
    IReadOnlyList<PlanningOperationColumn> Operations,
    IReadOnlyList<PlanningMatrixRow> Rows,
    IReadOnlyDictionary<string, PlanningOperationSummary> OperationSummaries);

public sealed record PlanningOperationColumn(
    int OperationId,
    string OperationCode,
    string OperationName,
    int SequenceNumber);

public sealed record PlanningMatrixRow(
    int PartNumberId,
    string PartNumber,
    string ProductFamily,
    string PartDescription,
    decimal DemandUnits,
    IReadOnlyDictionary<string, PlanningMatrixCell> Cells,
    IReadOnlyList<PlanningMatrixRow> ChildRows);

public sealed record PlanningMatrixCell(
    string OperationCode,
    string OperationName,
    decimal LoadHours,
    decimal EffectiveUnitsPerHour,
    decimal TimeCapacityUnits,
    decimal CapacityUnits,
    string? ConstraintName = null,
    string? ConstraintKind = null,
    int? RequiredResourceCount = null,
    int? AvailableResourceCount = null,
    int? MissingResourceCount = null,
    bool IsConstrained = false);

public sealed record PlanningOperationSummary(
    string OperationCode,
    string OperationName,
    decimal AvailableHours,
    decimal LoadHours,
    decimal RemainingHours,
    decimal LoadPercent);
