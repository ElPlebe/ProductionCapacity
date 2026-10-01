using ProductionCapacity.Data.ExcelImport;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace ProductionCapacity.Data.Tests;

public sealed class FmdSmtWorkbookImportParserTests
{
    [Fact]
    public void ReaderLoadsWorksheetValuesFromXlsxStream()
    {
        using var stream = new MemoryStream();
        using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook, true))
        {
            var workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook(new Sheets());
            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            worksheetPart.Worksheet = new Worksheet(new SheetData(
                new Row(
                    TextCell("A1", "BU"),
                    TextCell("B1", "FAMILY")),
                new Row(
                    TextCell("A2", "ERBU"),
                    TextCell("B2", "AZTECA"))));

            workbookPart.Workbook.GetFirstChild<Sheets>()!.Append(new Sheet
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "VOL"
            });
            workbookPart.Workbook.Save();
        }

        stream.Position = 0;

        var workbook = FmdSmtWorkbookReader.Read(stream);

        Assert.True(workbook.TryGetSheet("VOL", out var rows));
        Assert.Equal("BU", rows[0][0]);
        Assert.Equal("AZTECA", rows[1][1]);
    }

    [Fact]
    public void BuildsSmtImportPackageFromFmdWorkbookSheets()
    {
        var workbook = new FmdSmtWorkbook(
            new Dictionary<string, IReadOnlyList<IReadOnlyList<string>>>(StringComparer.OrdinalIgnoreCase)
            {
                ["VOL"] =
                [
                    ["BU", "FAMILY", "SUB FAMILY", "TAN PN", "MPS", "M1", "M2", "M3"],
                    ["ERBU", "Router", "AZTECA", "68-100", "40", "10", "20", "10"],
                    ["ERBU", "Router", "AZTECA", "68-101", "60", "15", "30", "15"]
                ],
                ["UPH"] =
                [
                    ["ACT", "FAMILY", " ", "TAN PN", "Change over", "BOTTLENECK", "ROUTER", "WAVE", "PRESSFIT"],
                    ["SMT", "Router", "AZTECA", "68-100", "2", "120", "180", "140", "90"]
                ],
                ["% Distribution"] =
                [
                    ["", "", "SMT M1", "", "", "", "", "", "", "SMT M2", "", "", "", "", "", ""],
                    ["BU", "FAMILY", "Line 1", "Line 2", "Line 3", "Line 4", "Line 5", "Line 6", "Total", "Line 1", "Line 2", "Line 3", "Line 4", "Line 5", "Line 6", "Total"],
                    ["ERBU", "AZTECA", "1", "0", "0", "0", "0", "0", "1", "0.5", "0.5", "0", "0", "0", "0", "1"]
                ],
                ["Capacity SMT"] =
                [
                    ["", "", "", "", "Week", "Month", "Asig. Month", "", "", "", "Day", "Week", "Month", "Month", "", "", "Month", "", "", "SMT 1", "M1"],
                    ["", "LINE", "FAMILY", "Line distribution", "CISCO MPS (W)", "CISCO MPS (M)", "CISCO MPS (A M)", "SMT UPH", "CO Month", "CO Hrs Month", "MAX CAP (D) 100%", "MAX CAP (W) 100%", "MAX CAP (M) 100%", "MAX CAP (A M)", "SMT Utilization", "SMT UTILIZATION MAX", "Delta Cap"],
                    ["", "1", "AZTECA", "1", "7.063", "28.25", "28.25", "120", "2", "8", "0", "0", "0", "100", "0.2825", "0.85", "71.75"],
                    ["", "", "TOTAL", "", "", "", "", "", "", "", "", "", "", "100", "0.2825", "0.85", "71.75"]
                ]
            });

        var package = FmdSmtWorkbookImportParser.Parse(workbook, new FmdSmtImportOptions("Base FMD SMT"));

        Assert.Collection(
            package.ProductionWeeks,
            first => Assert.Equal("M1", first.WeekName),
            second => Assert.Equal("M2", second.WeekName),
            third => Assert.Equal("M3", third.WeekName));

        Assert.Equal(2, package.PartNumbers.Count);
        Assert.All(package.PartNumbers, part => Assert.Equal("AZTECA", part.ProductFamily));

        var m1Volume = Assert.Single(package.SmtFamilyVolumes, row => row.WeekName == "M1");
        Assert.Equal(25m, m1Volume.MonthlyMps);
        Assert.Equal(6.25m, m1Volume.WeeklyMps);
        Assert.Equal(6.25m, package.WeeklyDemands.Single(row => row.WeekName == "M1").DemandUnits);

        var rate = Assert.Single(package.SmtFamilyRates);
        Assert.Equal("AZTECA", rate.ProductFamily);
        Assert.Equal(120m, rate.UnitsPerHour);
        Assert.Equal(2m, rate.ChangeoverCount);
        Assert.Equal(180m, rate.RouterUnitsPerHour);
        Assert.Equal(140m, rate.WaveUnitsPerHour);
        Assert.Equal(90m, rate.PressfitUnitsPerHour);

        Assert.Collection(
            package.SmtLineAssignments.OrderBy(row => row.MonthSequence).ThenBy(row => row.LineNumber),
            first =>
            {
                Assert.Equal("M1", first.WeekName);
                Assert.Equal("SMT 1", first.LineCode);
                Assert.Equal(1m, first.LineDistribution);
            },
            second =>
            {
                Assert.Equal("M2", second.WeekName);
                Assert.Equal("SMT 1", second.LineCode);
                Assert.Equal(0.5m, second.LineDistribution);
            },
            third =>
            {
                Assert.Equal("M2", third.WeekName);
                Assert.Equal("SMT 2", third.LineCode);
                Assert.Equal(0.5m, third.LineDistribution);
            });

        var target = Assert.Single(package.SmtCapacityTargets);
        Assert.Equal("M1", target.WeekName);
        Assert.Equal("SMT", target.OperationCode);
        Assert.Equal("SMT 1", target.LineCode);
        Assert.Equal("AZTECA", target.ProductFamily);
        Assert.Equal(28.25m, target.ExpectedAssignedDemandUnits);
        Assert.Equal(100m, target.ExpectedMaxCapacityUnits);
        Assert.Equal(28.25m, target.ExpectedUtilizationPercent);
        Assert.Equal(71.75m, target.ExpectedDeltaCapacityUnits);
        Assert.Contains("Capacity SMT", target.SourceReference);
    }

    [Fact]
    public void ConsolidatesDuplicateSmtLineAssignmentsFromDistributionSheet()
    {
        var workbook = new FmdSmtWorkbook(
            new Dictionary<string, IReadOnlyList<IReadOnlyList<string>>>(StringComparer.OrdinalIgnoreCase)
            {
                ["VOL"] =
                [
                    ["BU", "FAMILY", "SUB FAMILY", "TAN PN", "MPS", "M1"],
                    ["ERBU", "Router", "AZTECA", "68-100", "40", "10"]
                ],
                ["UPH"] =
                [
                    ["ACT", "FAMILY", " ", "TAN PN", "Change over", "BOTTLENECK"],
                    ["SMT", "Router", "AZTECA", "68-100", "2", "120"]
                ],
                ["% Distribution"] =
                [
                    ["", "", "SMT M1", "", "", "", "", "", ""],
                    ["BU", "FAMILY", "Line 1", "Line 2", "Line 3", "Line 4", "Line 5", "Line 6", "Total"],
                    ["ERBU", "AZTECA", "1", "0", "0", "0", "0", "0", "1"],
                    ["ERBU", "AZTECA", "1", "0", "0", "0", "0", "0", "1"]
                ]
            });

        var package = FmdSmtWorkbookImportParser.Parse(workbook, new FmdSmtImportOptions("Base FMD SMT"));

        var assignment = Assert.Single(package.SmtLineAssignments);
        Assert.Equal("AZTECA", assignment.ProductFamily);
        Assert.Equal("SMT 1", assignment.LineCode);
        Assert.Equal(1m, assignment.LineDistribution);
    }

    [Fact]
    public void UsesFiscalQuarterConfigurationForMonthLabels()
    {
        var workbook = new FmdSmtWorkbook(
            new Dictionary<string, IReadOnlyList<IReadOnlyList<string>>>(StringComparer.OrdinalIgnoreCase)
            {
                ["VOL"] =
                [
                    ["BU", "FAMILY", "SUB FAMILY", "TAN PN", "MPS", "M1", "M2", "M3"],
                    ["ERBU", "Router", "AZTECA", "68-100", "40", "10", "20", "30"]
                ],
                ["UPH"] =
                [
                    ["ACT", "FAMILY", " ", "TAN PN", "Change over", "BOTTLENECK"],
                    ["SMT", "Router", "AZTECA", "68-100", "2", "120"]
                ],
                ["% Distribution"] =
                [
                    ["", "", "SMT M1", "", "", "", "", "", "", "SMT M2", "", "", "", "", "", "", "SMT M3"],
                    ["BU", "FAMILY", "Line 1", "Line 2", "Line 3", "Line 4", "Line 5", "Line 6", "Total", "Line 1", "Line 2", "Line 3", "Line 4", "Line 5", "Line 6", "Total", "Line 1"],
                    ["ERBU", "AZTECA", "1", "0", "0", "0", "0", "0", "1", "1", "0", "0", "0", "0", "0", "1", "1"]
                ]
            });

        var package = FmdSmtWorkbookImportParser.Parse(
            workbook,
            new FmdSmtImportOptions("Base FMD SMT", FiscalQuarter: 2, FiscalYear: 2026));

        Assert.Collection(
            package.SmtFamilyVolumes.OrderBy(row => row.MonthSequence),
            m1 => Assert.Equal("Noviembre 2026", m1.MonthName),
            m2 => Assert.Equal("Diciembre 2026", m2.MonthName),
            m3 => Assert.Equal("Enero 2027", m3.MonthName));
        Assert.Collection(
            package.SmtLineAssignments.OrderBy(row => row.MonthSequence),
            m1 => Assert.Equal("Noviembre 2026", m1.MonthName),
            m2 => Assert.Equal("Diciembre 2026", m2.MonthName),
            m3 => Assert.Equal("Enero 2027", m3.MonthName));
    }

    [Fact]
    public void UsesConfigSheetFiscalQuarterWhenPresent()
    {
        var workbook = new FmdSmtWorkbook(
            new Dictionary<string, IReadOnlyList<IReadOnlyList<string>>>(StringComparer.OrdinalIgnoreCase)
            {
                ["CONFIG"] =
                [
                    ["FiscalYear", "FiscalQuarter"],
                    ["2026", "Q2"]
                ],
                ["VOL"] =
                [
                    ["BU", "FAMILY", "SUB FAMILY", "TAN PN", "MPS", "M1", "M2", "M3"],
                    ["ERBU", "Router", "AZTECA", "68-100", "40", "10", "20", "30"]
                ],
                ["UPH"] =
                [
                    ["ACT", "FAMILY", " ", "TAN PN", "Change over", "BOTTLENECK"],
                    ["SMT", "Router", "AZTECA", "68-100", "2", "120"]
                ],
                ["% Distribution"] =
                [
                    ["", "", "SMT M1", "", "", "", "", "", "", "SMT M2", "", "", "", "", "", "", "SMT M3"],
                    ["BU", "FAMILY", "Line 1", "Line 2", "Line 3", "Line 4", "Line 5", "Line 6", "Total", "Line 1", "Line 2", "Line 3", "Line 4", "Line 5", "Line 6", "Total", "Line 1"],
                    ["ERBU", "AZTECA", "1", "0", "0", "0", "0", "0", "1", "1", "0", "0", "0", "0", "0", "1", "1"]
                ]
            });

        var config = FmdSmtWorkbookImportParser.ReadConfig(workbook);
        var package = FmdSmtWorkbookImportParser.Parse(
            workbook,
            new FmdSmtImportOptions("Base FMD SMT", FiscalQuarter: 1, FiscalYear: 2025));

        Assert.True(config.HasFiscalCalendar);
        Assert.Equal(2, config.FiscalQuarter);
        Assert.Equal(2026, config.FiscalYear);
        Assert.Collection(
            package.SmtFamilyVolumes.OrderBy(row => row.MonthSequence),
            m1 => Assert.Equal("Noviembre 2026", m1.MonthName),
            m2 => Assert.Equal("Diciembre 2026", m2.MonthName),
            m3 => Assert.Equal("Enero 2027", m3.MonthName));
    }

    [Fact]
    public void CalculatesSmtUphFromStandardTimeYieldAndEfficiencyWhenBottleneckIsMissing()
    {
        var workbook = new FmdSmtWorkbook(
            new Dictionary<string, IReadOnlyList<IReadOnlyList<string>>>(StringComparer.OrdinalIgnoreCase)
            {
                ["VOL"] =
                [
                    ["BU", "FAMILY", "SUB FAMILY", "TAN PN", "MPS", "M1"],
                    ["ERBU", "Router", "AZTECA", "68-100", "40", "10"]
                ],
                ["UPH"] =
                [
                    ["ACT", "FAMILY", "SUB FAMILY", "TAN PN", "Change over", "StandardTimeSeconds", "YieldFactor", "EfficiencyFactor"],
                    ["SMT", "Router", "AZTECA", "68-100", "0", "1237.59", "0.88", "0.85"]
                ],
                ["% Distribution"] =
                [
                    ["", "", "SMT M1", "", "", "", "", "", ""],
                    ["BU", "FAMILY", "Line 1", "Line 2", "Line 3", "Line 4", "Line 5", "Line 6", "Total"],
                    ["ERBU", "AZTECA", "1", "0", "0", "0", "0", "0", "1"]
                ]
            });

        var package = FmdSmtWorkbookImportParser.Parse(workbook, new FmdSmtImportOptions("Base FMD SMT"));

        var rate = Assert.Single(package.SmtFamilyRates);
        Assert.Equal("AZTECA", rate.ProductFamily);
        Assert.Equal(2.18m, rate.UnitsPerHour);
        Assert.Equal(rate.UnitsPerHour, rate.BottleneckUnitsPerHour);
    }

    private static Cell TextCell(string reference, string value) => new()
    {
        CellReference = reference,
        DataType = CellValues.String,
        CellValue = new CellValue(value)
    };
}
