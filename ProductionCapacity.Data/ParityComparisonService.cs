namespace ProductionCapacity.Data;

public sealed class ParityComparisonService(PlanningMatrixService matrixService)
{
    private static readonly IReadOnlyList<PlanUabuSample> Samples =
    [
        new("WK13", "68-103450", "ASSY", 20.519m, 254.7m, 228.35m)
    ];

    public async Task<ParityComparisonResult> ComparePlanUabuAsync(
        string scenarioName = "Base semanal",
        decimal tolerance = 0.05m,
        CancellationToken cancellationToken = default)
    {
        var matrices = await matrixService.GetMatricesAsync(scenarioName, cancellationToken);
        var rows = new List<ParityComparisonRow>();

        foreach (var sample in Samples)
        {
            var matrix = matrices.SingleOrDefault(item => item.WeekName == sample.WeekName);
            if (matrix is null)
            {
                rows.Add(ParityComparisonRow.Missing(sample, "Semana no encontrada en el sistema."));
                continue;
            }

            var partRow = matrix.Rows
                .SelectMany(item => item.ChildRows.Count == 0 ? [item] : item.ChildRows)
                .SingleOrDefault(item => item.PartNumber == sample.PartNumber);
            if (partRow is null)
            {
                rows.Add(ParityComparisonRow.Missing(sample, "Numero de parte no encontrado en la matriz."));
                continue;
            }

            if (!partRow.Cells.TryGetValue(sample.OperationCode, out var cell))
            {
                rows.Add(ParityComparisonRow.Missing(sample, "Operacion no encontrada para el numero de parte."));
                continue;
            }

            if (!matrix.OperationSummaries.TryGetValue(sample.OperationCode, out var summary))
            {
                rows.Add(ParityComparisonRow.Missing(sample, "Resumen de operacion no encontrado."));
                continue;
            }

            rows.Add(new ParityComparisonRow(
                sample.WeekName,
                sample.PartNumber,
                sample.OperationCode,
                sample.ExpectedLoadHours,
                Math.Round(cell.LoadHours, 3, MidpointRounding.AwayFromZero),
                sample.ExpectedAvailableHours,
                Math.Round(summary.AvailableHours, 3, MidpointRounding.AwayFromZero),
                sample.ExpectedRemainingHours,
                Math.Round(summary.RemainingHours, 3, MidpointRounding.AwayFromZero),
                null,
                IsWithinTolerance(sample.ExpectedLoadHours, cell.LoadHours, tolerance) &&
                IsWithinTolerance(sample.ExpectedAvailableHours, summary.AvailableHours, tolerance) &&
                IsWithinTolerance(sample.ExpectedRemainingHours, summary.RemainingHours, tolerance)));
        }

        return new ParityComparisonResult(scenarioName, tolerance, rows);
    }

    private static bool IsWithinTolerance(decimal expected, decimal actual, decimal tolerance) =>
        Math.Abs(expected - actual) <= tolerance;
}

public sealed record ParityComparisonResult(
    string ScenarioName,
    decimal Tolerance,
    IReadOnlyList<ParityComparisonRow> Rows)
{
    public bool IsPassing => Rows.Count > 0 && Rows.All(row => row.IsPassing);
}

public sealed record ParityComparisonRow(
    string WeekName,
    string PartNumber,
    string OperationCode,
    decimal ExpectedLoadHours,
    decimal? ActualLoadHours,
    decimal ExpectedAvailableHours,
    decimal? ActualAvailableHours,
    decimal ExpectedRemainingHours,
    decimal? ActualRemainingHours,
    string? Message,
    bool IsPassing)
{
    public static ParityComparisonRow Missing(PlanUabuSample sample, string message) => new(
        sample.WeekName,
        sample.PartNumber,
        sample.OperationCode,
        sample.ExpectedLoadHours,
        null,
        sample.ExpectedAvailableHours,
        null,
        sample.ExpectedRemainingHours,
        null,
        message,
        false);
}

public sealed record PlanUabuSample(
    string WeekName,
    string PartNumber,
    string OperationCode,
    decimal ExpectedLoadHours,
    decimal ExpectedAvailableHours,
    decimal ExpectedRemainingHours);
