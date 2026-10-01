using System.Globalization;

namespace ProductionCapacity.Data.ExcelImport;

public sealed record CsvImportFile(string FileName, string Content);

public static class CsvCapacityImportParser
{
    public static ExcelCapacityImportPackage Parse(IEnumerable<CsvImportFile> files)
    {
        ArgumentNullException.ThrowIfNull(files);

        var scenarios = new List<ExcelScenarioImportRow>();
        var productionWeeks = new List<ExcelProductionWeekImportRow>();
        var partNumbers = new List<ExcelPartNumberImportRow>();
        var weeklyDemand = new List<ExcelWeeklyDemandImportRow>();
        var operations = new List<ExcelOperationImportRow>();
        var operationRates = new List<ExcelOperationRateImportRow>();
        var operationCapacities = new List<ExcelOperationCapacityImportRow>();
        var crossReferences = new List<ExcelCrossReferenceImportRow>();
        var testTimes = new List<ExcelTestTimeImportRow>();
        var resourceTypes = new List<ExcelResourceTypeImportRow>();
        var resourceConfigurations = new List<ExcelResourceConfigurationImportRow>();
        var resourceInventories = new List<ExcelResourceInventoryImportRow>();
        var smtFamilyVolumes = new List<ExcelSmtFamilyVolumeImportRow>();
        var smtFamilyRates = new List<ExcelSmtFamilyRateImportRow>();
        var smtLineFamilyRates = new List<ExcelSmtLineFamilyRateImportRow>();
        var smtLineAssignments = new List<ExcelSmtLineAssignmentImportRow>();
        var smtCapacityTargets = new List<ExcelSmtCapacityTargetImportRow>();
        var sourceWarnings = new List<string>();

        foreach (var file in files)
        {
            var fileName = NormalizeFileName(file.FileName);
            if (!KnownFileNames.Contains(fileName))
            {
                sourceWarnings.Add($"El archivo '{file.FileName}' no coincide con una plantilla reconocida y no fue importado.");
                continue;
            }

            var rows = ReadRows(file.Content).ToList();
            if (rows.Count == 0)
            {
                continue;
            }

            var headers = rows[0]
                .Select((name, index) => new { Name = NormalizeHeader(name), Index = index })
                .Where(header => !string.IsNullOrWhiteSpace(header.Name))
                .ToDictionary(header => header.Name, header => header.Index, StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows.Skip(1).Where(row => row.Any(value => !string.IsNullOrWhiteSpace(value))))
            {
                switch (fileName)
                {
                    case "scenarios.csv":
                        scenarios.Add(new ExcelScenarioImportRow(
                            Get(row, headers, "ScenarioName"),
                            EmptyToNull(Get(row, headers, "Description", required: false)),
                            ParseBool(Get(row, headers, "IsBaseline", required: false))));
                        break;
                    case "production_weeks.csv":
                        productionWeeks.Add(new ExcelProductionWeekImportRow(
                            Get(row, headers, "ScenarioName"),
                            Get(row, headers, "WeekName"),
                            ParseDate(Get(row, headers, "WeekStartDate", required: false)),
                            ParseDecimal(Get(row, headers, "WorkDays")),
                            ParseDecimal(Get(row, headers, "HoursPerShift")),
                            ParseInt(Get(row, headers, "ShiftsPerDay")),
                            ParseDecimal(Get(row, headers, "HolidayDays", required: false)),
                            ParseDecimal(Get(row, headers, "OvertimeHours", required: false)),
                            ParseDecimal(Get(row, headers, "PlannedDowntimeHours", required: false))));
                        break;
                    case "part_numbers.csv":
                        partNumbers.Add(new ExcelPartNumberImportRow(
                            Get(row, headers, "PartNumber"),
                            Get(row, headers, "Description"),
                            EmptyToNull(Get(row, headers, "BusinessUnit", required: false)),
                            EmptyToNull(Get(row, headers, "ProductFamily", required: false)),
                            ParseDecimal(Get(row, headers, "DefaultYield", required: false, defaultValue: "1")),
                            EmptyToNull(Get(row, headers, "SubFamily", required: false))));
                        break;
                    case "weekly_demand.csv":
                        weeklyDemand.Add(new ExcelWeeklyDemandImportRow(
                            Get(row, headers, "ScenarioName"),
                            Get(row, headers, "WeekName"),
                            EmptyToNull(Get(row, headers, "PartNumber", required: false)),
                            EmptyToNull(Get(row, headers, "ProductFamily", required: false)),
                            ParseDecimal(Get(row, headers, "DemandUnits"))));
                        break;
                    case "operations.csv":
                        operations.Add(new ExcelOperationImportRow(
                            Get(row, headers, "OperationCode"),
                            Get(row, headers, "OperationName"),
                            EmptyToNull(Get(row, headers, "OperationFamily", required: false)),
                            ParseInt(Get(row, headers, "SequenceNumber", required: false, defaultValue: "1"))));
                        break;
                    case "operation_rates.csv":
                        operationRates.Add(new ExcelOperationRateImportRow(
                            EmptyToNull(Get(row, headers, "PartNumber", required: false)),
                            EmptyToNull(Get(row, headers, "ProductFamily", required: false)),
                            Get(row, headers, "OperationCode"),
                            ParseDecimal(Get(row, headers, "StandardTimeSeconds")),
                            ParseDecimal(Get(row, headers, "YieldFactor", required: false, defaultValue: "1")),
                            ParseDecimal(Get(row, headers, "UtilizationFactor", required: false, defaultValue: "1")),
                            ParseDecimal(Get(row, headers, "EfficiencyFactor", required: false, defaultValue: "1")),
                            ParseNullableDecimal(Get(row, headers, "MaxCapacityUnits", required: false)),
                            ParseNullableDecimal(Get(row, headers, "FixedHeadcount", required: false))));
                        break;
                    case "operation_capacities.csv":
                        operationCapacities.Add(new ExcelOperationCapacityImportRow(
                            Get(row, headers, "ScenarioName"),
                            Get(row, headers, "WeekName"),
                            Get(row, headers, "OperationCode"),
                            ParseDecimal(Get(row, headers, "WorkDays")),
                            ParseDecimal(Get(row, headers, "HoursPerShift")),
                            ParseInt(Get(row, headers, "ShiftsPerDay")),
                            ParseDecimal(Get(row, headers, "EquipmentQuantity")),
                            ParseDecimal(Get(row, headers, "HolidayDays", required: false)),
                            ParseDecimal(Get(row, headers, "OvertimeHours", required: false)),
                            ParseDecimal(Get(row, headers, "PlannedDowntimeHours", required: false))));
                        break;
                    case "cross_references.csv":
                        crossReferences.Add(new ExcelCrossReferenceImportRow(
                            Get(row, headers, "PartNumber"),
                            Get(row, headers, "ReferenceType"),
                            Get(row, headers, "ReferenceValue")));
                        break;
                    case "test_times.csv":
                        testTimes.Add(new ExcelTestTimeImportRow(
                            EmptyToNull(Get(row, headers, "PartNumber", required: false)),
                            EmptyToNull(Get(row, headers, "ProductFamily", required: false)),
                            Get(row, headers, "OperationCode"),
                            Get(row, headers, "TestName"),
                            ParseDecimal(Get(row, headers, "TestTimeMinutes")),
                            ParseDecimal(Get(row, headers, "HandlingMinutes", required: false)),
                            ParseDecimal(Get(row, headers, "QuantityCell", required: false, defaultValue: "1")),
                            ParseDecimal(Get(row, headers, "QuantityEquipment", required: false, defaultValue: "1")),
                            ParseNullableDecimal(Get(row, headers, "ManualStandardTimeSeconds", required: false))));
                        break;
                    case "resource_types.csv":
                        resourceTypes.Add(new ExcelResourceTypeImportRow(
                            Get(row, headers, "ResourceCode"),
                            Get(row, headers, "ResourceName"),
                            Get(row, headers, "ResourceKind")));
                        break;
                    case "resource_configurations.csv":
                        resourceConfigurations.Add(new ExcelResourceConfigurationImportRow(
                            Get(row, headers, "ResourceCode"),
                            Get(row, headers, "OperationCode"),
                            EmptyToNull(Get(row, headers, "PartNumber", required: false)),
                            EmptyToNull(Get(row, headers, "ProductFamily", required: false)),
                            ParseDecimal(Get(row, headers, "UnitsPerResource")),
                            EmptyToNull(Get(row, headers, "Notes", required: false))));
                        break;
                    case "resource_inventories.csv":
                        resourceInventories.Add(new ExcelResourceInventoryImportRow(
                            Get(row, headers, "ScenarioName"),
                            Get(row, headers, "WeekName"),
                            Get(row, headers, "ResourceCode"),
                            ParseInt(Get(row, headers, "AvailableResourceCount")),
                            EmptyToNull(Get(row, headers, "Notes", required: false))));
                        break;
                    case "smt_family_volumes.csv":
                        smtFamilyVolumes.Add(new ExcelSmtFamilyVolumeImportRow(
                            Get(row, headers, "ScenarioName"),
                            Get(row, headers, "WeekName"),
                            EmptyToNull(Get(row, headers, "BusinessUnit", required: false)),
                            Get(row, headers, "ProductFamily"),
                            EmptyToNull(Get(row, headers, "MonthName", required: false)),
                            ParseInt(Get(row, headers, "MonthSequence", required: false, defaultValue: "0")),
                            ParseDecimal(Get(row, headers, "MonthlyMps", required: false)),
                            ParseDecimal(Get(row, headers, "WeeklyMps", required: false))));
                        break;
                    case "smt_family_rates.csv":
                        smtFamilyRates.Add(new ExcelSmtFamilyRateImportRow(
                            Get(row, headers, "ProductFamily"),
                            Get(row, headers, "OperationCode"),
                            ParseDecimal(Get(row, headers, "UnitsPerHour")),
                            ParseDecimal(Get(row, headers, "ChangeoverCount", required: false)),
                            EmptyToNull(Get(row, headers, "SourcePartNumber", required: false)),
                            ParseNullableDecimal(Get(row, headers, "BottleneckUnitsPerHour", required: false)),
                            ParseNullableDecimal(Get(row, headers, "RouterUnitsPerHour", required: false)),
                            ParseNullableDecimal(Get(row, headers, "WaveUnitsPerHour", required: false)),
                            ParseNullableDecimal(Get(row, headers, "PressfitUnitsPerHour", required: false))));
                        break;
                    case "smt_line_family_rates.csv":
                        smtLineFamilyRates.Add(new ExcelSmtLineFamilyRateImportRow(
                            Get(row, headers, "ProductFamily"),
                            Get(row, headers, "OperationCode"),
                            Get(row, headers, "LineCode"),
                            ParseInt(Get(row, headers, "LineNumber", required: false, defaultValue: "0")),
                            ParseDecimal(Get(row, headers, "UnitsPerHour")),
                            EmptyToNull(Get(row, headers, "SourcePartNumber", required: false)),
                            ParseNullableDecimal(Get(row, headers, "BottleneckUnitsPerHour", required: false)),
                            ParseNullableDecimal(Get(row, headers, "RouterUnitsPerHour", required: false)),
                            ParseNullableDecimal(Get(row, headers, "WaveUnitsPerHour", required: false)),
                            ParseNullableDecimal(Get(row, headers, "PressfitUnitsPerHour", required: false))));
                        break;
                    case "smt_line_assignments.csv":
                        smtLineAssignments.Add(new ExcelSmtLineAssignmentImportRow(
                            Get(row, headers, "ScenarioName"),
                            Get(row, headers, "WeekName"),
                            Get(row, headers, "ProductFamily"),
                            Get(row, headers, "OperationCode"),
                            Get(row, headers, "LineCode"),
                            ParseInt(Get(row, headers, "LineNumber", required: false, defaultValue: "0")),
                            ParseDecimal(Get(row, headers, "LineDistribution", required: false, defaultValue: "1")),
                            EmptyToNull(Get(row, headers, "MonthName", required: false)),
                            ParseInt(Get(row, headers, "MonthSequence", required: false, defaultValue: "0"))));
                        break;
                    case "smt_capacity_targets.csv":
                        smtCapacityTargets.Add(new ExcelSmtCapacityTargetImportRow(
                            Get(row, headers, "ScenarioName"),
                            Get(row, headers, "WeekName"),
                            Get(row, headers, "ProductFamily"),
                            Get(row, headers, "OperationCode"),
                            Get(row, headers, "LineCode"),
                            ParseDecimal(Get(row, headers, "ExpectedAssignedDemandUnits")),
                            ParseDecimal(Get(row, headers, "ExpectedMaxCapacityUnits")),
                            ParseDecimal(Get(row, headers, "ExpectedUtilizationPercent")),
                            ParseDecimal(Get(row, headers, "ExpectedDeltaCapacityUnits")),
                            EmptyToNull(Get(row, headers, "SourceReference", required: false)),
                            EmptyToNull(Get(row, headers, "Notes", required: false))));
                        break;
                    case "source_warnings.csv":
                        sourceWarnings.Add(Get(row, headers, "Warning"));
                        break;
                }
            }
        }

        return new ExcelCapacityImportPackage(
            scenarios,
            productionWeeks,
            partNumbers,
            weeklyDemand,
            operations,
            operationRates,
            operationCapacities,
            crossReferences,
            testTimes,
            resourceTypes,
            resourceConfigurations,
            resourceInventories,
            smtFamilyVolumes,
            smtFamilyRates,
            smtLineFamilyRates,
            smtLineAssignments,
            smtCapacityTargets,
            sourceWarnings);
    }

    private static IEnumerable<IReadOnlyList<string>> ReadRows(string content)
    {
        using var reader = new StringReader(content);
        while (reader.ReadLine() is { } line)
        {
            yield return SplitCsvLine(line);
        }
    }

    private static IReadOnlyList<string> SplitCsvLine(string line)
    {
        var values = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"')
            {
                if (inQuotes && index + 1 < line.Length && line[index + 1] == '"')
                {
                    current.Append('"');
                    index++;
                    continue;
                }

                inQuotes = !inQuotes;
                continue;
            }

            if (character == ',' && !inQuotes)
            {
                values.Add(current.ToString().Trim());
                current.Clear();
                continue;
            }

            current.Append(character);
        }

        values.Add(current.ToString().Trim());
        return values;
    }

    private static string Get(
        IReadOnlyList<string> row,
        IReadOnlyDictionary<string, int> headers,
        string header,
        bool required = true,
        string defaultValue = "")
    {
        var key = NormalizeHeader(header);
        if (!headers.TryGetValue(key, out var index) || index >= row.Count)
        {
            if (required)
            {
                throw new ArgumentException($"La columna requerida '{header}' no existe en el archivo CSV.");
            }

            return defaultValue;
        }

        var value = row[index].Trim();
        if (required && string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"La columna requerida '{header}' viene vacia en el archivo CSV.");
        }

        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }

    private static decimal ParseDecimal(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? 0m
            : decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture);

    private static decimal? ParseNullableDecimal(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : ParseDecimal(value);

    private static int ParseInt(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? 0
            : int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);

    private static bool ParseBool(string value) =>
        value.Trim().ToLowerInvariant() switch
        {
            "true" or "1" or "yes" or "si" or "sí" => true,
            _ => false
        };

    private static DateOnly? ParseDate(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : DateOnly.Parse(value, CultureInfo.InvariantCulture);

    private static string NormalizeHeader(string value) =>
        value.Trim().Replace("_", string.Empty, StringComparison.OrdinalIgnoreCase);

    private static string NormalizeFileName(string fileName) =>
        Path.GetFileName(fileName).Trim().ToLowerInvariant();

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static readonly HashSet<string> KnownFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "scenarios.csv",
        "production_weeks.csv",
        "part_numbers.csv",
        "weekly_demand.csv",
        "operations.csv",
        "operation_rates.csv",
        "operation_capacities.csv",
        "cross_references.csv",
        "test_times.csv",
        "resource_types.csv",
        "resource_configurations.csv",
        "resource_inventories.csv",
        "smt_family_volumes.csv",
        "smt_family_rates.csv",
        "smt_line_family_rates.csv",
        "smt_line_assignments.csv",
        "smt_capacity_targets.csv",
        "source_warnings.csv"
    };
}
