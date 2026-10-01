using DocumentFormat.OpenXml.Packaging;

namespace ProductionCapacity.Data.Tests;

public sealed class SmtTemplateWorkbookTests
{
    [Fact]
    public void FmdSmtTemplateContainsGuidanceAndRequiredSmtCaptureColumns()
    {
        var templatePath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "import-templates",
            "fmd_smt_template.xlsx"));

        using var document = SpreadsheetDocument.Open(templatePath, false);
        var sheetNames = document.WorkbookPart!.Workbook.Sheets!.Elements<DocumentFormat.OpenXml.Spreadsheet.Sheet>()
            .Select(sheet => sheet.Name!.Value!)
            .ToList();

        Assert.Contains("INSTRUCCIONES", sheetNames);

        var uphText = ReadSheetText(document, "UPH");
        Assert.Contains("StandardTimeSeconds", uphText);
        Assert.Contains("YieldFactor", uphText);
        Assert.Contains("EfficiencyFactor", uphText);

        var distributionText = ReadSheetText(document, "% Distribution");
        Assert.Contains("Captura porcentajes", distributionText);
    }

    private static string ReadSheetText(SpreadsheetDocument document, string sheetName)
    {
        var workbookPart = document.WorkbookPart!;
        var sheet = workbookPart.Workbook.Sheets!.Elements<DocumentFormat.OpenXml.Spreadsheet.Sheet>()
            .Single(item => item.Name == sheetName);
        var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
        var sharedStrings = workbookPart.SharedStringTablePart?.SharedStringTable;
        var values = worksheetPart.Worksheet.Descendants<DocumentFormat.OpenXml.Spreadsheet.Cell>()
            .Select(cell => ReadCell(cell, sharedStrings))
            .Where(value => !string.IsNullOrWhiteSpace(value));

        return string.Join("|", values);
    }

    private static string ReadCell(
        DocumentFormat.OpenXml.Spreadsheet.Cell cell,
        DocumentFormat.OpenXml.Spreadsheet.SharedStringTable? sharedStrings)
    {
        var value = cell.CellValue?.InnerText ?? string.Empty;
        if (cell.DataType?.Value == DocumentFormat.OpenXml.Spreadsheet.CellValues.SharedString &&
            int.TryParse(value, out var sharedStringIndex) &&
            sharedStrings is not null)
        {
            return sharedStrings.ElementAt(sharedStringIndex).InnerText;
        }

        return value;
    }
}
