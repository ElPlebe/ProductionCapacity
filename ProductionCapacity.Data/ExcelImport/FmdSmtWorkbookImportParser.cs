using System.Globalization;
using System.Text.RegularExpressions;

namespace ProductionCapacity.Data.ExcelImport;

public sealed record FmdSmtImportOptions(
    string ScenarioName,
    int FiscalQuarter = 1,
    int FiscalYear = 0,
    decimal WorkDays = 5.66m,
    decimal HoursPerShift = 7.5m,
    int ShiftsPerDay = 3,
    decimal EquipmentQuantity = 6m,
    decimal HolidayDays = 0m,
    decimal OvertimeHours = 0m,
    decimal PlannedDowntimeHours = 0m);

public sealed record FmdSmtWorkbookConfig(int? FiscalQuarter, int? FiscalYear)
{
    public bool HasFiscalCalendar => FiscalQuarter is >= 1 and <= 4 && FiscalYear is >= 2000 and <= 2100;
}

public sealed class FmdSmtWorkbook(IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyList<string>>> sheets)
{
    public bool TryGetSheet(string sheetName, out IReadOnlyList<IReadOnlyList<string>> rows) =>
        sheets.TryGetValue(sheetName, out rows!);
}

public static partial class FmdSmtWorkbookImportParser
{
    private const string OperationCode = "SMT";
    private const string OperationName = "Surface Mount";

    public static ExcelCapacityImportPackage Parse(FmdSmtWorkbook workbook, FmdSmtImportOptions options)
    {
        ArgumentNullException.ThrowIfNull(workbook);
        ArgumentNullException.ThrowIfNull(options);

        var warnings = new List<string>();
        options = ApplyConfig(workbook, options, warnings);
        var volumeResult = ParseVolumes(workbook, options, warnings);
        var rateRows = ParseRates(workbook, warnings);
        var assignmentRows = ParseAssignments(workbook, options, warnings);
        var capacityTargetRows = ParseCapacityTargets(workbook, options, warnings);
        var monthSequences = volumeResult.Volumes
            .Select(row => row.MonthSequence)
            .Concat(assignmentRows.Select(row => row.MonthSequence))
            .Concat(capacityTargetRows.Select(row => ParseMonthSequence(row.WeekName)))
            .Where(sequence => sequence > 0)
            .DefaultIfEmpty(1)
            .Distinct()
            .Order()
            .ToList();

        foreach (var family in volumeResult.Volumes.Select(row => row.ProductFamily).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!rateRows.Any(row => row.ProductFamily.Equals(family, StringComparison.OrdinalIgnoreCase)))
            {
                warnings.Add($"FMD SMT: la familia '{family}' tiene volumen en VOL pero no tiene UPH en la hoja UPH.");
            }
        }

        return new ExcelCapacityImportPackage(
            Scenarios:
            [
                new ExcelScenarioImportRow(options.ScenarioName, "Importado desde FMD SMT", true)
            ],
            ProductionWeeks: monthSequences
                .Select(sequence => new ExcelProductionWeekImportRow(
                    options.ScenarioName,
                    MonthName(sequence),
                    null,
                    options.WorkDays,
                    options.HoursPerShift,
                    options.ShiftsPerDay,
                    options.HolidayDays,
                    options.OvertimeHours,
                    options.PlannedDowntimeHours))
                .ToList(),
            PartNumbers: volumeResult.PartNumbers,
            WeeklyDemands: volumeResult.WeeklyDemands,
            Operations:
            [
                new ExcelOperationImportRow(OperationCode, OperationName, OperationCode, 1)
            ],
            OperationRates: [],
            OperationCapacities: monthSequences
                .Select(sequence => new ExcelOperationCapacityImportRow(
                    options.ScenarioName,
                    MonthName(sequence),
                    OperationCode,
                    options.WorkDays,
                    options.HoursPerShift,
                    options.ShiftsPerDay,
                    options.EquipmentQuantity,
                    options.HolidayDays,
                    options.OvertimeHours,
                    options.PlannedDowntimeHours))
                .ToList(),
            CrossReferences: [],
            TestTimes: [],
            ResourceTypes: [],
            ResourceConfigurations: [],
            ResourceInventories: [],
            SmtFamilyVolumes: volumeResult.Volumes,
            SmtFamilyRates: rateRows,
            SmtLineFamilyRates: [],
            SmtLineAssignments: assignmentRows,
            SmtCapacityTargets: capacityTargetRows,
            SourceWarnings: warnings.Distinct(StringComparer.OrdinalIgnoreCase).ToList());
    }

    public static FmdSmtWorkbookConfig ReadConfig(FmdSmtWorkbook workbook)
    {
        ArgumentNullException.ThrowIfNull(workbook);

        if (!workbook.TryGetSheet("CONFIG", out var rows) || rows.Count == 0)
        {
            return new FmdSmtWorkbookConfig(null, null);
        }

        var tableConfig = ReadConfigFromTable(rows);
        if (tableConfig.HasFiscalCalendar)
        {
            return tableConfig;
        }

        return ReadConfigFromKeyValueRows(rows);
    }

    private static FmdSmtImportOptions ApplyConfig(
        FmdSmtWorkbook workbook,
        FmdSmtImportOptions options,
        List<string> warnings)
    {
        var config = ReadConfig(workbook);
        if (!config.HasFiscalCalendar)
        {
            return options;
        }

        warnings.Add($"FMD SMT: CONFIG detectado. Se usara Q{config.FiscalQuarter} {config.FiscalYear} para M1/M2/M3.");
        return options with
        {
            FiscalQuarter = config.FiscalQuarter!.Value,
            FiscalYear = config.FiscalYear!.Value
        };
    }

    private static FmdSmtWorkbookConfig ReadConfigFromTable(IReadOnlyList<IReadOnlyList<string>> rows)
    {
        var headerIndex = FindHeaderRow(rows, "FiscalYear", "FiscalQuarter");
        if (headerIndex < 0 || headerIndex + 1 >= rows.Count)
        {
            return new FmdSmtWorkbookConfig(null, null);
        }

        var headers = BuildHeaderMap(rows[headerIndex]);
        var year = ParseInt(Get(rows[headerIndex + 1], FindColumn(headers, "FiscalYear", "Fiscal Year", "AnioFiscal", "AñoFiscal")));
        var quarter = ParseFiscalQuarter(Get(rows[headerIndex + 1], FindColumn(headers, "FiscalQuarter", "Fiscal Quarter", "Quarter", "CuartoFiscal", "Cuarto Fiscal")));
        return new FmdSmtWorkbookConfig(quarter, year);
    }

    private static FmdSmtWorkbookConfig ReadConfigFromKeyValueRows(IReadOnlyList<IReadOnlyList<string>> rows)
    {
        int? quarter = null;
        int? year = null;
        foreach (var row in rows)
        {
            if (row.Count < 2)
            {
                continue;
            }

            var key = NormalizeHeader(row[0]);
            if (key is "FISCALYEAR" or "FISCALANIO" or "ANIOFISCAL" or "AÑOFISCAL")
            {
                year = ParseInt(Get(row, 1));
            }
            else if (key is "FISCALQUARTER" or "QUARTER" or "CUARTOFISCAL")
            {
                quarter = ParseFiscalQuarter(Get(row, 1));
            }
        }

        return new FmdSmtWorkbookConfig(quarter, year);
    }

    private static FmdVolumeParseResult ParseVolumes(
        FmdSmtWorkbook workbook,
        FmdSmtImportOptions options,
        List<string> warnings)
    {
        if (!workbook.TryGetSheet("VOL", out var rows))
        {
            warnings.Add("FMD SMT: no se encontro la hoja VOL.");
            return new FmdVolumeParseResult([], [], []);
        }

        var headerIndex = FindHeaderRow(rows, "SUB FAMILY", "M1");
        if (headerIndex < 0)
        {
            warnings.Add("FMD SMT: no se encontraron encabezados SUB FAMILY/M1 en la hoja VOL.");
            return new FmdVolumeParseResult([], [], []);
        }

        var headers = BuildHeaderMap(rows[headerIndex]);
        var buColumn = FindColumn(headers, "BU");
        var familyColumn = FindColumn(headers, "SUB FAMILY", "FAMILY");
        var tanColumn = FindColumn(headers, "TAN PN", "TANPN", "PART NUMBER");
        var monthColumns = headers
            .Where(pair => MonthLabelRegex().IsMatch(pair.Key))
            .Select(pair => new { Month = ParseMonthSequence(pair.Key), Column = pair.Value })
            .Where(pair => pair.Month > 0)
            .OrderBy(pair => pair.Month)
            .ToList();

        var parts = new Dictionary<string, ExcelPartNumberImportRow>(StringComparer.OrdinalIgnoreCase);
        var volumes = new Dictionary<(string Family, int Month), FmdVolumeAggregate>();

        foreach (var row in rows.Skip(headerIndex + 1))
        {
            var family = Get(row, familyColumn);
            if (string.IsNullOrWhiteSpace(family))
            {
                continue;
            }

            family = family.Trim();
            var businessUnit = Get(row, buColumn);
            var tanPartNumber = Get(row, tanColumn);
            if (!string.IsNullOrWhiteSpace(tanPartNumber) && !parts.ContainsKey(tanPartNumber))
            {
                parts.Add(tanPartNumber, new ExcelPartNumberImportRow(
                    tanPartNumber,
                    $"{family} {tanPartNumber}",
                    EmptyToNull(businessUnit),
                    family,
                    1m,
                    family));
            }

            foreach (var monthColumn in monthColumns)
            {
                var periodVolume = ParseDecimal(Get(row, monthColumn.Column));
                if (periodVolume <= 0)
                {
                    continue;
                }

                var key = (family, monthColumn.Month);
                if (!volumes.TryGetValue(key, out var aggregate))
                {
                    aggregate = new FmdVolumeAggregate(null, 0m);
                }

                volumes[key] = aggregate.Add(EmptyToNull(businessUnit), periodVolume);
            }
        }

        var volumeRows = volumes
            .OrderBy(pair => pair.Key.Month)
            .ThenBy(pair => pair.Key.Family, StringComparer.OrdinalIgnoreCase)
            .Select(pair => new ExcelSmtFamilyVolumeImportRow(
                options.ScenarioName,
                MonthName(pair.Key.Month),
                pair.Value.BusinessUnit,
                pair.Key.Family,
                FiscalMonthLabel(options, pair.Key.Month),
                pair.Key.Month,
                pair.Value.Volume,
                pair.Value.Volume / FiscalQuarterCalendar.GetPeriod(
                    options.FiscalQuarter,
                    options.FiscalYear <= 0 ? DateTime.Today.Year : options.FiscalYear,
                    pair.Key.Month).WeekCount))
            .ToList();

        var demandRows = volumeRows
            .Select(row => new ExcelWeeklyDemandImportRow(
                row.ScenarioName,
                row.WeekName,
                null,
                row.ProductFamily,
                row.WeeklyMps))
            .ToList();

        return new FmdVolumeParseResult(parts.Values.OrderBy(row => row.PartNumber).ToList(), volumeRows, demandRows);
    }

    private static IReadOnlyList<ExcelSmtFamilyRateImportRow> ParseRates(FmdSmtWorkbook workbook, List<string> warnings)
    {
        if (!workbook.TryGetSheet("UPH", out var rows))
        {
            warnings.Add("FMD SMT: no se encontro la hoja UPH.");
            return [];
        }

        var headerIndex = FindHeaderRow(rows, "BOTTLENECK");
        if (headerIndex < 0)
        {
            headerIndex = FindHeaderRow(rows, "StandardTimeSeconds");
        }

        if (headerIndex < 0)
        {
            warnings.Add("FMD SMT: no se encontro el encabezado BOTTLENECK o StandardTimeSeconds en la hoja UPH.");
            return [];
        }

        var headers = BuildHeaderMap(rows[headerIndex]);
        var sourcePartColumn = FindColumn(headers, "TAN PN", "TANPN", "PART NUMBER");
        var familyColumn = FindUphFamilyColumn(headers, sourcePartColumn);
        var changeoverColumn = FindColumn(headers, "CHANGE OVER", "CHANGEOVER", "CO");
        var bottleneckColumn = FindColumn(headers, "BOTTLENECK", "SMT UPH", "UPH");
        var standardTimeColumn = FindColumn(headers, "StandardTimeSeconds", "Standard Time Seconds", "STANDAR_TIME", "STANDARDTIME");
        var yieldColumn = FindColumn(headers, "YieldFactor", "Yield", "YIELD FACTOR");
        var efficiencyColumn = FindColumn(headers, "EfficiencyFactor", "Efficiency", "EFFICIENCY FACTOR");
        var routerColumn = FindColumn(headers, "ROUTER");
        var waveColumn = FindColumn(headers, "WAVE");
        var pressfitColumn = FindColumn(headers, "PRESSFIT");
        var byFamily = new Dictionary<string, ExcelSmtFamilyRateImportRow>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows.Skip(headerIndex + 1))
        {
            var family = Get(row, familyColumn);
            if (string.IsNullOrWhiteSpace(family) || byFamily.ContainsKey(family))
            {
                continue;
            }

            var bottleneck = ParseUnitsPerHour(row, bottleneckColumn, standardTimeColumn, yieldColumn, efficiencyColumn);
            if (bottleneck <= 0)
            {
                continue;
            }

            byFamily.Add(family, new ExcelSmtFamilyRateImportRow(
                family.Trim(),
                OperationCode,
                bottleneck,
                ParseDecimal(Get(row, changeoverColumn)),
                EmptyToNull(Get(row, sourcePartColumn)),
                bottleneck,
                ParseNullableDecimal(Get(row, routerColumn)),
                ParseNullableDecimal(Get(row, waveColumn)),
                ParseNullableDecimal(Get(row, pressfitColumn))));
        }

        return byFamily.Values.OrderBy(row => row.ProductFamily, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static IReadOnlyList<ExcelSmtLineAssignmentImportRow> ParseAssignments(
        FmdSmtWorkbook workbook,
        FmdSmtImportOptions options,
        List<string> warnings)
    {
        if (!workbook.TryGetSheet("% Distribution", out var rows))
        {
            warnings.Add("FMD SMT: no se encontro la hoja % Distribution.");
            return [];
        }

        var headerIndex = FindHeaderRow(rows, "FAMILY", "Line 1");
        if (headerIndex < 0)
        {
            warnings.Add("FMD SMT: no se encontraron encabezados FAMILY/Line 1 en la hoja % Distribution.");
            return [];
        }

        var headerRow = rows[headerIndex];
        var groupRow = headerIndex > 0 ? rows[headerIndex - 1] : [];
        var headers = BuildHeaderMap(headerRow);
        var familyColumn = FindColumn(headers, "FAMILY", "SUB FAMILY");
        var lineColumns = BuildLineColumns(headerRow, groupRow);
        var assignments = new List<ExcelSmtLineAssignmentImportRow>();

        foreach (var row in rows.Skip(headerIndex + 1))
        {
            var family = Get(row, familyColumn);
            if (string.IsNullOrWhiteSpace(family))
            {
                continue;
            }

            foreach (var lineColumn in lineColumns)
            {
                var distribution = ParseDecimal(Get(row, lineColumn.Column));
                if (distribution <= 0)
                {
                    continue;
                }

                assignments.Add(new ExcelSmtLineAssignmentImportRow(
                    options.ScenarioName,
                    MonthName(lineColumn.MonthSequence),
                    family.Trim(),
                    OperationCode,
                    $"SMT {lineColumn.LineNumber}",
                    lineColumn.LineNumber,
                    distribution,
                    FiscalMonthLabel(options, lineColumn.MonthSequence),
                    lineColumn.MonthSequence));
            }
        }

        return assignments
            .GroupBy(
                row => string.Join('|', row.ScenarioName, row.WeekName, row.ProductFamily, row.OperationCode, row.LineCode),
                StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var first = group.First();
                return first with
                {
                    LineDistribution = group.Max(row => row.LineDistribution)
                };
            })
            .OrderBy(row => row.MonthSequence)
            .ThenBy(row => row.LineNumber)
            .ThenBy(row => row.ProductFamily, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<ExcelSmtCapacityTargetImportRow> ParseCapacityTargets(
        FmdSmtWorkbook workbook,
        FmdSmtImportOptions options,
        List<string> warnings)
    {
        if (!workbook.TryGetSheet("Capacity SMT", out var rows))
        {
            warnings.Add("FMD SMT: no se encontro la hoja Capacity SMT; la comparacion 1:1 quedara sin targets Excel.");
            return [];
        }

        var targets = new List<ExcelSmtCapacityTargetImportRow>();
        for (var headerIndex = 0; headerIndex < rows.Count; headerIndex++)
        {
            var headerRow = rows[headerIndex];
            if (!IsCapacityTargetHeaderRow(headerRow))
            {
                continue;
            }

            var groupRow = headerIndex > 0 ? rows[headerIndex - 1] : [];
            var blockStarts = headerRow
                .Select((value, index) => new { Header = NormalizeHeader(value), Index = index })
                .Where(item => item.Header == NormalizeHeader("LINE"))
                .Select(item => item.Index)
                .ToList();

            for (var blockIndex = 0; blockIndex < blockStarts.Count; blockIndex++)
            {
                var blockStart = blockStarts[blockIndex];
                var blockEnd = blockIndex + 1 < blockStarts.Count
                    ? blockStarts[blockIndex + 1]
                    : Math.Max(headerRow.Count, groupRow.Count);
                var headers = BuildHeaderMap(headerRow, blockStart, blockEnd);
                var lineColumn = FindColumn(headers, "LINE");
                var familyColumn = FindColumn(headers, "FAMILY");
                var distributionColumn = FindColumn(headers, "Line distribution");
                var assignedColumn = FindColumn(headers, "CISCO MPS (A M)");
                var maxCapacityColumn = FindColumn(headers, "MAX CAP (A M)");
                var utilizationColumn = FindColumn(headers, "SMT Utilization");
                var deltaColumn = FindColumn(headers, "Delta Cap");
                var monthSequence = FindMonthSequence(groupRow, blockStart, blockEnd);

                if (lineColumn < 0 ||
                    familyColumn < 0 ||
                    distributionColumn < 0 ||
                    assignedColumn < 0 ||
                    maxCapacityColumn < 0 ||
                    utilizationColumn < 0 ||
                    deltaColumn < 0 ||
                    monthSequence <= 0)
                {
                    continue;
                }

                var lineCode = FindLineCode(groupRow, blockStart, blockEnd);
                for (var rowIndex = headerIndex + 1; rowIndex < rows.Count; rowIndex++)
                {
                    var row = rows[rowIndex];
                    if (IsCapacityTargetHeaderRow(row))
                    {
                        break;
                    }

                    var family = Get(row, familyColumn);
                    if (string.IsNullOrWhiteSpace(family))
                    {
                        continue;
                    }

                    if (family.Equals("TOTAL", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    var distribution = ParseDecimal(Get(row, distributionColumn));
                    var assignedDemand = ParseDecimal(Get(row, assignedColumn));
                    if (distribution <= 0m && assignedDemand <= 0m)
                    {
                        continue;
                    }

                    var rowLineCode = ResolveLineCode(lineCode, Get(row, lineColumn));
                    if (string.IsNullOrWhiteSpace(rowLineCode))
                    {
                        continue;
                    }

                    targets.Add(new ExcelSmtCapacityTargetImportRow(
                        options.ScenarioName,
                        MonthName(monthSequence),
                        family.Trim(),
                        OperationCode,
                        rowLineCode,
                        assignedDemand,
                        ParseDecimal(Get(row, maxCapacityColumn)),
                        NormalizePercentForSystem(ParseDecimal(Get(row, utilizationColumn))),
                        ParseDecimal(Get(row, deltaColumn)),
                        $"Capacity SMT!{ColumnName(blockStart)}{rowIndex + 1}:{ColumnName(Math.Max(blockEnd - 1, blockStart))}{rowIndex + 1}",
                        "Target importado desde la hoja Capacity SMT."));
                }
            }
        }

        return targets
            .OrderBy(row => row.WeekName)
            .ThenBy(row => row.LineCode)
            .ThenBy(row => row.ProductFamily, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsCapacityTargetHeaderRow(IReadOnlyList<string> row)
    {
        var headers = row
            .Select(NormalizeHeader)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return headers.Contains(NormalizeHeader("LINE")) &&
            headers.Contains(NormalizeHeader("FAMILY")) &&
            headers.Contains(NormalizeHeader("CISCO MPS (A M)")) &&
            headers.Contains(NormalizeHeader("MAX CAP (A M)")) &&
            headers.Contains(NormalizeHeader("SMT Utilization")) &&
            headers.Contains(NormalizeHeader("Delta Cap"));
    }

    private static int FindMonthSequence(IReadOnlyList<string> row, int startInclusive, int endExclusive)
    {
        for (var column = startInclusive; column < endExclusive; column++)
        {
            var monthSequence = ParseMonthSequence(Get(row, column));
            if (monthSequence > 0)
            {
                return monthSequence;
            }
        }

        return 0;
    }

    private static string FindLineCode(IReadOnlyList<string> row, int startInclusive, int endExclusive)
    {
        for (var column = startInclusive; column < endExclusive; column++)
        {
            var value = Get(row, column);
            if (value.StartsWith("SMT", StringComparison.OrdinalIgnoreCase))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }

    private static string ResolveLineCode(string lineCode, string lineValue)
    {
        if (!string.IsNullOrWhiteSpace(lineCode))
        {
            return lineCode;
        }

        return int.TryParse(lineValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var lineNumber) && lineNumber > 0
            ? $"SMT {lineNumber}"
            : string.Empty;
    }

    private static decimal NormalizePercentForSystem(decimal value) =>
        value is > 0m and <= 1m ? value * 100m : value;

    private static IReadOnlyList<FmdLineColumn> BuildLineColumns(
        IReadOnlyList<string> headerRow,
        IReadOnlyList<string> groupRow)
    {
        var lineColumns = new List<FmdLineColumn>();
        var activeMonth = 0;

        for (var column = 0; column < headerRow.Count; column++)
        {
            var groupLabel = Get(groupRow, column);
            var groupMonth = ParseMonthSequence(groupLabel);
            if (groupMonth > 0)
            {
                activeMonth = groupMonth;
            }

            var lineMatch = LineHeaderRegex().Match(Get(headerRow, column));
            if (!lineMatch.Success || activeMonth <= 0)
            {
                continue;
            }

            lineColumns.Add(new FmdLineColumn(
                column,
                activeMonth,
                int.Parse(lineMatch.Groups[1].Value, CultureInfo.InvariantCulture)));
        }

        return lineColumns;
    }

    private static int FindHeaderRow(IReadOnlyList<IReadOnlyList<string>> rows, params string[] requiredHeaders)
    {
        for (var index = 0; index < rows.Count; index++)
        {
            var headerText = rows[index]
                .Select(NormalizeHeader)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (requiredHeaders.All(header => headerText.Contains(NormalizeHeader(header))))
            {
                return index;
            }
        }

        return -1;
    }

    private static Dictionary<string, int> BuildHeaderMap(IReadOnlyList<string> row)
    {
        return BuildHeaderMap(row, 0, row.Count);
    }

    private static Dictionary<string, int> BuildHeaderMap(IReadOnlyList<string> row, int startInclusive, int endExclusive)
    {
        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var index = startInclusive; index < Math.Min(endExclusive, row.Count); index++)
        {
            var header = NormalizeHeader(row[index]);
            if (!string.IsNullOrWhiteSpace(header) && !headers.ContainsKey(header))
            {
                headers.Add(header, index);
            }
        }

        return headers;
    }

    private static int FindColumn(IReadOnlyDictionary<string, int> headers, params string[] names)
    {
        foreach (var name in names)
        {
            if (headers.TryGetValue(NormalizeHeader(name), out var column))
            {
                return column;
            }
        }

        return -1;
    }

    private static int FindUphFamilyColumn(IReadOnlyDictionary<string, int> headers, int sourcePartColumn)
    {
        var subFamilyColumn = FindColumn(headers, "SUB FAMILY", "SUBFAMILY");
        if (subFamilyColumn >= 0)
        {
            return subFamilyColumn;
        }

        var familyColumn = FindColumn(headers, "FAMILY");
        if (familyColumn >= 0 && sourcePartColumn > familyColumn + 1)
        {
            return familyColumn + 1;
        }

        return familyColumn;
    }

    private static string Get(IReadOnlyList<string> row, int column) =>
        column < 0 || column >= row.Count ? string.Empty : row[column]?.Trim() ?? string.Empty;

    private static decimal? ParseNullableDecimal(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : ParseDecimal(value);

    private static decimal ParseUnitsPerHour(
        IReadOnlyList<string> row,
        int unitsPerHourColumn,
        int standardTimeSecondsColumn,
        int yieldFactorColumn,
        int efficiencyFactorColumn)
    {
        var unitsPerHour = ParseDecimal(Get(row, unitsPerHourColumn));
        if (unitsPerHour > 0m)
        {
            return unitsPerHour;
        }

        var standardTimeSeconds = ParseDecimal(Get(row, standardTimeSecondsColumn));
        if (standardTimeSeconds <= 0m)
        {
            return 0m;
        }

        var yieldFactor = ParseFactor(Get(row, yieldFactorColumn));
        var efficiencyFactor = ParseFactor(Get(row, efficiencyFactorColumn));

        return Math.Round(3600m * yieldFactor * efficiencyFactor / standardTimeSeconds, 2, MidpointRounding.AwayFromZero);
    }

    private static decimal ParseFactor(string value)
    {
        var factor = ParseDecimal(value);
        if (factor <= 0m)
        {
            return 1m;
        }

        return factor > 1m ? factor / 100m : factor;
    }

    private static decimal ParseDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0m;
        }

        var text = value.Trim();
        var isPercent = text.EndsWith('%');
        if (isPercent)
        {
            text = text[..^1];
        }

        if (text.Contains(',') && !text.Contains('.'))
        {
            text = text.Replace(',', '.');
        }
        else
        {
            text = text.Replace(",", string.Empty, StringComparison.Ordinal);
        }

        if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
        {
            return 0m;
        }

        return isPercent ? number / 100m : number;
    }

    private static int ParseMonthSequence(string value)
    {
        var match = MonthLabelRegex().Match(value);
        return match.Success ? int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) : 0;
    }

    private static int ParseInt(string value) =>
        int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
            ? number
            : 0;

    private static int ParseFiscalQuarter(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        var match = QuarterLabelRegex().Match(value);
        return match.Success
            ? int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture)
            : ParseInt(value);
    }

    private static string MonthName(int monthSequence) => $"M{monthSequence}";

    private static string FiscalMonthLabel(FmdSmtImportOptions options, int monthSequence)
    {
        var fiscalYear = options.FiscalYear <= 0 ? DateTime.Today.Year : options.FiscalYear;
        return FiscalQuarterCalendar.GetPeriod(options.FiscalQuarter, fiscalYear, monthSequence).MonthLabel;
    }

    private static string NormalizeHeader(string value) =>
        value.Trim()
            .Replace("_", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("%", string.Empty, StringComparison.OrdinalIgnoreCase)
            .ToUpperInvariant();

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string ColumnName(int zeroBasedColumn)
    {
        var dividend = zeroBasedColumn + 1;
        var columnName = string.Empty;
        while (dividend > 0)
        {
            var modulo = (dividend - 1) % 26;
            columnName = Convert.ToChar('A' + modulo) + columnName;
            dividend = (dividend - modulo) / 26;
        }

        return columnName;
    }

    [GeneratedRegex(@"M\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MonthLabelRegex();

    [GeneratedRegex(@"Q\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex QuarterLabelRegex();

    [GeneratedRegex(@"^LINE\s*(\d+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LineHeaderRegex();

    private sealed record FmdVolumeParseResult(
        IReadOnlyList<ExcelPartNumberImportRow> PartNumbers,
        IReadOnlyList<ExcelSmtFamilyVolumeImportRow> Volumes,
        IReadOnlyList<ExcelWeeklyDemandImportRow> WeeklyDemands);

    private sealed record FmdVolumeAggregate(string? BusinessUnit, decimal Volume)
    {
        public FmdVolumeAggregate Add(string? businessUnit, decimal volume) =>
            new(BusinessUnit ?? businessUnit, Volume + volume);
    }

    private sealed record FmdLineColumn(int Column, int MonthSequence, int LineNumber);
}
