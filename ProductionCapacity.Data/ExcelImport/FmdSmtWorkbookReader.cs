using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace ProductionCapacity.Data.ExcelImport;

public static partial class FmdSmtWorkbookReader
{
    public static FmdSmtWorkbook Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        using var document = SpreadsheetDocument.Open(stream, false);
        var workbookPart = document.WorkbookPart ?? throw new InvalidOperationException("El archivo XLSX no contiene workbook.");
        var sharedStrings = workbookPart.SharedStringTablePart?.SharedStringTable;
        var sheets = new Dictionary<string, IReadOnlyList<IReadOnlyList<string>>>(StringComparer.OrdinalIgnoreCase);

        foreach (var sheet in workbookPart.Workbook.Sheets?.Elements<Sheet>() ?? [])
        {
            if (sheet.Id?.Value is not { Length: > 0 } relationshipId ||
                workbookPart.GetPartById(relationshipId) is not WorksheetPart worksheetPart ||
                sheet.Name?.Value is not { Length: > 0 } sheetName)
            {
                continue;
            }

            var rows = new List<IReadOnlyList<string>>();
            foreach (var row in worksheetPart.Worksheet.Descendants<Row>())
            {
                var values = new List<string>();
                foreach (var cell in row.Elements<Cell>())
                {
                    var columnIndex = GetZeroBasedColumnIndex(cell.CellReference?.Value);
                    while (values.Count <= columnIndex)
                    {
                        values.Add(string.Empty);
                    }

                    values[columnIndex] = ReadCellValue(cell, sharedStrings);
                }

                rows.Add(values);
            }

            sheets[sheetName] = rows;
        }

        return new FmdSmtWorkbook(sheets);
    }

    private static string ReadCellValue(Cell cell, SharedStringTable? sharedStrings)
    {
        if (cell.DataType?.Value == CellValues.SharedString &&
            int.TryParse(cell.CellValue?.InnerText, out var sharedStringIndex) &&
            sharedStrings is not null)
        {
            return sharedStrings.ElementAt(sharedStringIndex).InnerText;
        }

        if (cell.DataType?.Value == CellValues.InlineString)
        {
            return cell.InlineString?.Text?.Text ?? cell.InnerText;
        }

        return cell.CellValue?.InnerText ?? cell.InnerText ?? string.Empty;
    }

    private static int GetZeroBasedColumnIndex(string? cellReference)
    {
        if (string.IsNullOrWhiteSpace(cellReference))
        {
            return 0;
        }

        var match = CellReferenceRegex().Match(cellReference);
        if (!match.Success)
        {
            return 0;
        }

        var columnIndex = 0;
        foreach (var character in match.Groups[1].Value)
        {
            columnIndex *= 26;
            columnIndex += char.ToUpperInvariant(character) - 'A' + 1;
        }

        return columnIndex - 1;
    }

    [GeneratedRegex(@"^([A-Z]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CellReferenceRegex();
}
