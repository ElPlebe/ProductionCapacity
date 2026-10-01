using ProductionCapacity.Data.ExcelImport;

namespace ProductionCapacity.Data.Tests;

public sealed class CsvCapacityImportParserTests
{
    [Fact]
    public void ParsesCsvFilesIntoImportPackage()
    {
        var files = new[]
        {
            new CsvImportFile("operations.csv", "OperationCode,OperationName,OperationFamily,SequenceNumber\r\nASSY,ASSY,Assembly,1\r\n"),
            new CsvImportFile("part_numbers.csv", "PartNumber,Description,BusinessUnit,ProductFamily,SubFamily,DefaultYield\r\n68-103450,Longball,UABU,Longball,Longball SMT,1\r\n"),
            new CsvImportFile("weekly_demand.csv", "ScenarioName,WeekName,PartNumber,DemandUnits\r\nBase semanal,WK13,68-103450,1000\r\n"),
            new CsvImportFile("operation_rates.csv", "PartNumber,ProductFamily,OperationCode,StandardTimeSeconds,YieldFactor,UtilizationFactor,EfficiencyFactor,MaxCapacityUnits,FixedHeadcount\r\n68-103450,,ASSY,73.8684,1,1,1,,\r\n"),
            new CsvImportFile("source_warnings.csv", "Warning\r\nPLAN UABU tiene #REF!\r\n")
        };

        var package = CsvCapacityImportParser.Parse(files);

        Assert.Single(package.Operations);
        Assert.Single(package.PartNumbers);
        Assert.Equal("Longball SMT", Assert.Single(package.PartNumbers).SubFamily);
        Assert.Single(package.WeeklyDemands);
        Assert.Single(package.OperationRates);
        Assert.Contains(package.SourceWarnings, warning => warning.Contains("#REF!"));
    }

    [Fact]
    public void WarnsWhenCsvFileNameIsNotRecognized()
    {
        var package = CsvCapacityImportParser.Parse(
        [
            new CsvImportFile("PLAN UABU.csv", "PartNumber,DemandUnits\r\n68-103450,1000\r\n")
        ]);

        Assert.Contains(package.SourceWarnings, warning => warning.Contains("PLAN UABU.csv"));
    }

    [Fact]
    public void ParsesFamilyOperationRates()
    {
        var package = CsvCapacityImportParser.Parse(
        [
            new CsvImportFile("operation_rates.csv", "PartNumber,ProductFamily,OperationCode,StandardTimeSeconds,YieldFactor,UtilizationFactor,EfficiencyFactor,MaxCapacityUnits,FixedHeadcount\r\n,Azteca,SMT,120,1,0.85,1,,\r\n")
        ]);

        var rate = Assert.Single(package.OperationRates);
        Assert.Null(rate.PartNumber);
        Assert.Equal("Azteca", rate.ProductFamily);
        Assert.Equal("SMT", rate.OperationCode);
    }

    [Fact]
    public void ParsesSmtImportFiles()
    {
        var package = CsvCapacityImportParser.Parse(
        [
            new CsvImportFile("weekly_demand.csv", "ScenarioName,WeekName,PartNumber,ProductFamily,DemandUnits\r\nBase semanal,WK13,,AZTECA,28.25\r\n"),
            new CsvImportFile("smt_family_volumes.csv", "ScenarioName,WeekName,BusinessUnit,ProductFamily,MonthName,MonthSequence,MonthlyMps,WeeklyMps\r\nBase semanal,WK13,ERBU,AZTECA,AUG,1,113,28.25\r\n"),
            new CsvImportFile("smt_family_rates.csv", "ProductFamily,OperationCode,UnitsPerHour,ChangeoverCount,SourcePartNumber,BottleneckUnitsPerHour,RouterUnitsPerHour,WaveUnitsPerHour,PressfitUnitsPerHour\r\nAZTECA,SMT,2.176590656,4,,2.176590656,38.084415584,17,3.636363636\r\n"),
            new CsvImportFile("smt_line_family_rates.csv", "ProductFamily,OperationCode,LineCode,LineNumber,UnitsPerHour,SourcePartNumber,BottleneckUnitsPerHour,RouterUnitsPerHour,WaveUnitsPerHour,PressfitUnitsPerHour\r\nAZTECA,SMT,SMT 1,1,2.5,,2.5,38,17,3.6\r\n"),
            new CsvImportFile("smt_line_assignments.csv", "ScenarioName,WeekName,ProductFamily,OperationCode,LineCode,LineNumber,LineDistribution,MonthName,MonthSequence\r\nBase semanal,WK13,AZTECA,SMT,SMT 1,1,1,AUG,1\r\n")
        ]);

        var demand = Assert.Single(package.WeeklyDemands);
        Assert.Null(demand.PartNumber);
        Assert.Equal("AZTECA", demand.ProductFamily);
        Assert.Single(package.SmtFamilyVolumes);
        Assert.Single(package.SmtFamilyRates);
        var lineRate = Assert.Single(package.SmtLineFamilyRates);
        Assert.Equal("SMT 1", lineRate.LineCode);
        Assert.Equal(2.5m, lineRate.UnitsPerHour);
        Assert.Single(package.SmtLineAssignments);
    }
}
