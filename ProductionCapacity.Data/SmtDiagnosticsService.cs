using Microsoft.EntityFrameworkCore;

namespace ProductionCapacity.Data;

public enum SmtDiagnosticSeverity
{
    Info,
    Warning,
    Critical
}

public enum SmtDiagnosticStatus
{
    Ok,
    Warning,
    Critical
}

public sealed record SmtDiagnosticCard(
    string Title,
    SmtDiagnosticStatus Status,
    int IssueCount,
    string Summary);

public sealed record SmtDiagnosticIssue(
    SmtDiagnosticSeverity Severity,
    string Category,
    string EntityKey,
    string Message,
    string RecommendedAction);

public sealed record SmtDiagnosticsSnapshot(
    string ScenarioName,
    IReadOnlyList<SmtDiagnosticCard> Cards,
    IReadOnlyList<SmtDiagnosticIssue> Issues)
{
    public int CriticalCount => Issues.Count(issue => issue.Severity == SmtDiagnosticSeverity.Critical);
    public int WarningCount => Issues.Count(issue => issue.Severity == SmtDiagnosticSeverity.Warning);
}

public sealed class SmtDiagnosticsService(PlanningDbContext db)
{
    private const decimal DistributionTolerance = 0.001m;

    public async Task<SmtDiagnosticsSnapshot> GetSnapshotAsync(
        string scenarioName,
        CancellationToken cancellationToken = default)
    {
        var scenario = await db.Scenarios
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.ScenarioName == scenarioName, cancellationToken)
            ?? throw new InvalidOperationException($"Scenario '{scenarioName}' was not found.");

        var weeks = await db.ProductionWeeks
            .AsNoTracking()
            .Where(item => item.ScenarioId == scenario.ScenarioId)
            .Select(item => new WeekLookup(item.ProductionWeekId, item.WeekName))
            .ToListAsync(cancellationToken);
        var weekById = weeks.ToDictionary(item => item.ProductionWeekId);

        var operations = await db.Operations
            .AsNoTracking()
            .Select(item => new OperationLookup(item.OperationId, item.OperationCode))
            .ToListAsync(cancellationToken);
        var operationById = operations.ToDictionary(item => item.OperationId);

        var volumes = await db.SmtFamilyVolumes
            .AsNoTracking()
            .Where(item => item.ScenarioId == scenario.ScenarioId)
            .ToListAsync(cancellationToken);
        var assignments = await db.SmtLineAssignments
            .AsNoTracking()
            .Where(item => item.ScenarioId == scenario.ScenarioId && item.IsActive)
            .ToListAsync(cancellationToken);
        var familyRates = await db.SmtFamilyRates
            .AsNoTracking()
            .Where(item => item.IsActive && item.UnitsPerHour > 0m)
            .ToListAsync(cancellationToken);
        var capacities = await db.OperationWeeklyCapacities
            .AsNoTracking()
            .Where(item => item.ScenarioId == scenario.ScenarioId)
            .ToListAsync(cancellationToken);
        var targets = await db.SmtCapacityTargets
            .AsNoTracking()
            .Where(item => item.ScenarioId == scenario.ScenarioId)
            .ToListAsync(cancellationToken);

        var issues = new List<SmtDiagnosticIssue>();
        AddVolumeIssues(volumes, issues);
        AddRateIssues(volumes, assignments, familyRates, issues);
        AddDistributionIssues(volumes, assignments, weekById, issues);
        AddCapacityIssues(assignments, capacities, weekById, operationById, issues);
        AddTargetIssues(assignments, targets, weekById, issues);

        return new SmtDiagnosticsSnapshot(
            scenario.ScenarioName,
            BuildCards(issues),
            issues
                .OrderByDescending(issue => issue.Severity)
                .ThenBy(issue => issue.Category, StringComparer.OrdinalIgnoreCase)
                .ThenBy(issue => issue.EntityKey, StringComparer.OrdinalIgnoreCase)
                .ToList());
    }

    private static void AddVolumeIssues(
        IReadOnlyList<Entities.SmtFamilyVolumeEntity> volumes,
        List<SmtDiagnosticIssue> issues)
    {
        if (volumes.Count == 0)
        {
            issues.Add(new SmtDiagnosticIssue(
                SmtDiagnosticSeverity.Critical,
                "Volumen",
                "FMD SMT",
                "No hay volumen SMT cargado para el escenario.",
                "Importa la hoja VOL del FMD SMT o captura volumen antes de revisar capacidad."));
        }
    }

    private static void AddRateIssues(
        IReadOnlyList<Entities.SmtFamilyVolumeEntity> volumes,
        IReadOnlyList<Entities.SmtLineAssignmentEntity> assignments,
        IReadOnlyList<Entities.SmtFamilyRateEntity> familyRates,
        List<SmtDiagnosticIssue> issues)
    {
        var plannedFamilies = volumes
            .Select(item => item.ProductFamily)
            .Concat(assignments.Select(item => item.ProductFamily))
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item, StringComparer.OrdinalIgnoreCase);
        var familiesWithRates = familyRates
            .Select(item => item.ProductFamily)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var family in plannedFamilies.Where(family => !familiesWithRates.Contains(family)))
        {
            issues.Add(new SmtDiagnosticIssue(
                SmtDiagnosticSeverity.Critical,
                "UPH",
                family,
                $"La familia {family} tiene planeacion SMT, pero no tiene UPH activo.",
                "Captura UPH por familia en Admin SMT o importa la hoja UPH."));
        }
    }

    private static void AddDistributionIssues(
        IReadOnlyList<Entities.SmtFamilyVolumeEntity> volumes,
        IReadOnlyList<Entities.SmtLineAssignmentEntity> assignments,
        IReadOnlyDictionary<int, WeekLookup> weekById,
        List<SmtDiagnosticIssue> issues)
    {
        var assignmentGroups = assignments
            .GroupBy(item => new FamilyWeekKey(item.ProductionWeekId, NormalizeKey(item.ProductFamily)))
            .ToDictionary(
                group => group.Key,
                group => group.ToList());

        foreach (var volume in volumes.Where(item => item.MonthlyMps > 0m))
        {
            var key = new FamilyWeekKey(volume.ProductionWeekId, NormalizeKey(volume.ProductFamily));
            var weekName = GetWeekName(weekById, volume.ProductionWeekId);
            if (!assignmentGroups.TryGetValue(key, out var group) || group.Count == 0)
            {
                issues.Add(new SmtDiagnosticIssue(
                    SmtDiagnosticSeverity.Critical,
                    "Distribucion",
                    $"{volume.ProductFamily}|{weekName}",
                    $"La familia {volume.ProductFamily} tiene volumen en {weekName}, pero no tiene distribucion por linea.",
                    "Carga % Distribution o captura una asignacion de linea SMT."));
                continue;
            }

            var totalDistribution = group.Sum(item => item.LineDistribution);
            if (Math.Abs(totalDistribution - 1m) > DistributionTolerance)
            {
                issues.Add(new SmtDiagnosticIssue(
                    SmtDiagnosticSeverity.Critical,
                    "Distribucion",
                    $"{volume.ProductFamily}|{weekName}",
                    $"La distribucion de {volume.ProductFamily} en {weekName} suma {totalDistribution:P0}; debe sumar 100%.",
                    "Ajusta % Distribution para que la suma de lineas sea 100%."));
            }
        }
    }

    private static void AddCapacityIssues(
        IReadOnlyList<Entities.SmtLineAssignmentEntity> assignments,
        IReadOnlyList<Entities.OperationWeeklyCapacityEntity> capacities,
        IReadOnlyDictionary<int, WeekLookup> weekById,
        IReadOnlyDictionary<int, OperationLookup> operationById,
        List<SmtDiagnosticIssue> issues)
    {
        var capacityKeys = capacities
            .Select(item => (item.ProductionWeekId, item.OperationId))
            .ToHashSet();
        var requiredKeys = assignments
            .Select(item => (item.ProductionWeekId, item.OperationId))
            .Distinct();

        foreach (var key in requiredKeys.Where(key => !capacityKeys.Contains(key)))
        {
            var weekName = GetWeekName(weekById, key.ProductionWeekId);
            var operationCode = GetOperationCode(operationById, key.OperationId);
            issues.Add(new SmtDiagnosticIssue(
                SmtDiagnosticSeverity.Critical,
                "Capacidad",
                $"{operationCode}|{weekName}",
                $"No hay calendario/capacidad para {operationCode} en {weekName}.",
                "Carga CONFIG/FMD SMT o captura capacidad semanal de la operacion SMT."));
        }
    }

    private static void AddTargetIssues(
        IReadOnlyList<Entities.SmtLineAssignmentEntity> assignments,
        IReadOnlyList<Entities.SmtCapacityTargetEntity> targets,
        IReadOnlyDictionary<int, WeekLookup> weekById,
        List<SmtDiagnosticIssue> issues)
    {
        var targetKeys = targets
            .Select(item => (item.ProductionWeekId, item.OperationId, item.ProductFamily, item.LineCode))
            .ToHashSet();

        foreach (var assignment in assignments.Where(assignment =>
            !targetKeys.Contains((assignment.ProductionWeekId, assignment.OperationId, assignment.ProductFamily, assignment.LineCode))))
        {
            var weekName = GetWeekName(weekById, assignment.ProductionWeekId);
            issues.Add(new SmtDiagnosticIssue(
                SmtDiagnosticSeverity.Warning,
                "Targets Excel",
                $"{assignment.ProductFamily}|{weekName}|{assignment.LineCode}",
                $"No hay target de Capacity SMT para {assignment.ProductFamily} en {assignment.LineCode}/{weekName}.",
                "Carga la hoja Capacity SMT si quieres comparar sistema contra Excel 1:1."));
        }
    }

    private static IReadOnlyList<SmtDiagnosticCard> BuildCards(IReadOnlyList<SmtDiagnosticIssue> issues)
    {
        return
        [
            BuildCard("Volumen SMT", "Volumen", issues, "Hay volumen SMT cargado."),
            BuildCard("UPH por familia", "UPH", issues, "Todas las familias planeadas tienen UPH."),
            BuildCard("Distribucion por linea", "Distribucion", issues, "Las familias tienen linea y suman 100%."),
            BuildCard("Capacidad/calendario", "Capacidad", issues, "Las lineas tienen calendario de capacidad."),
            BuildCard("Targets Excel", "Targets Excel", issues, "Hay targets para comparacion contra Capacity SMT.")
        ];
    }

    private static SmtDiagnosticCard BuildCard(
        string title,
        string category,
        IReadOnlyList<SmtDiagnosticIssue> issues,
        string okSummary)
    {
        var categoryIssues = issues
            .Where(issue => issue.Category == category)
            .ToList();
        var status = categoryIssues.Any(issue => issue.Severity == SmtDiagnosticSeverity.Critical)
            ? SmtDiagnosticStatus.Critical
            : categoryIssues.Any(issue => issue.Severity == SmtDiagnosticSeverity.Warning)
                ? SmtDiagnosticStatus.Warning
                : SmtDiagnosticStatus.Ok;
        var summary = status == SmtDiagnosticStatus.Ok
            ? okSummary
            : categoryIssues[0].Message;

        return new SmtDiagnosticCard(title, status, categoryIssues.Count, summary);
    }

    private static string GetWeekName(IReadOnlyDictionary<int, WeekLookup> weekById, int productionWeekId) =>
        weekById.TryGetValue(productionWeekId, out var week) ? week.WeekName : $"Semana {productionWeekId}";

    private static string GetOperationCode(IReadOnlyDictionary<int, OperationLookup> operationById, int operationId) =>
        operationById.TryGetValue(operationId, out var operation) ? operation.OperationCode : $"Operacion {operationId}";

    private static string NormalizeKey(string value) =>
        value.Trim().ToUpperInvariant();

    private sealed record FamilyWeekKey(int ProductionWeekId, string ProductFamily);

    private sealed record WeekLookup(int ProductionWeekId, string WeekName);

    private sealed record OperationLookup(int OperationId, string OperationCode);
}
