using System.Globalization;
using ClosedXML.Excel;
using EducationalCenter.Application.Common.Exceptions;
using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Common.Models;

namespace EducationalCenter.Infrastructure.Documents;

internal sealed class ExcelService : IExcelService
{
    private const string DataSheetName = "Data";
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public byte[] CreateTemplate(ImportTemplate template)
    {
        var rtl = template.Language == "ar";
        string L(string ar, string en) => rtl ? ar : en;

        using var workbook = new XLWorkbook();

        // "Data": only the header row, so leftover sample rows can never be imported.
        var data = workbook.Worksheets.Add(DataSheetName);
        for (var i = 0; i < template.Columns.Count; i++)
        {
            // Text format, so phone numbers keep their leading zero and dates stay as typed.
            data.Column(i + 1).Style.NumberFormat.Format = "@";

            var cell = data.Cell(1, i + 1);
            cell.Value = template.Columns[i].Name;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = template.Columns[i].Required ? XLColor.LightYellow : XLColor.LightGray;
        }
        data.Columns(1, template.Columns.Count).Width = 24;
        data.SheetView.FreezeRows(1);

        // "Instructions": notes, the column table and sample rows.
        var help = workbook.Worksheets.Add("Instructions");
        help.RightToLeft = rtl;

        var row = 1;
        help.Cell(row, 1).Value = template.Title;
        help.Cell(row, 1).Style.Font.Bold = true;
        help.Cell(row, 1).Style.Font.FontSize = 14;
        row += 2;

        foreach (var note in template.Notes)
            help.Cell(row++, 1).Value = note;
        row++;

        help.Cell(row, 1).Value = L("العمود", "Column");
        help.Cell(row, 2).Value = L("مطلوب", "Required");
        help.Cell(row, 3).Value = L("الوصف", "Description");
        help.Range(row, 1, row, 3).Style.Font.Bold = true;
        help.Range(row, 1, row, 3).Style.Fill.BackgroundColor = XLColor.LightGray;
        row++;

        foreach (var column in template.Columns)
        {
            help.Cell(row, 1).Value = column.Name;
            help.Cell(row, 2).Value = column.Required ? L("نعم", "Yes") : L("لا", "No");
            help.Cell(row, 3).Value = column.Hint;
            row++;
        }
        row++;

        help.Cell(row, 1).Value = L("مثال", "Example");
        help.Cell(row, 1).Style.Font.Bold = true;
        row++;

        for (var i = 0; i < template.Columns.Count; i++)
        {
            help.Cell(row, i + 1).Value = template.Columns[i].Name;
            help.Cell(row, i + 1).Style.Font.Bold = true;
        }
        row++;

        foreach (var sample in template.SampleRows)
        {
            for (var i = 0; i < sample.Count; i++)
                help.Cell(row, i + 1).Value = sample[i];
            row++;
        }

        help.Columns(1, Math.Max(3, template.Columns.Count)).AdjustToContents(3, row);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public ImportSheet ReadSheet(Stream file, IReadOnlyList<string> expectedColumns)
    {
        // Copy first: the workbook reader needs a seekable stream and an upload may not be one.
        using var buffer = new MemoryStream();
        file.CopyTo(buffer);
        buffer.Position = 0;

        XLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(buffer);
        }
        catch (Exception)
        {
            throw new RequestValidationException("file", "The file is not a valid .xlsx workbook.");
        }

        using (workbook)
        {
            var sheet = workbook.Worksheets.FirstOrDefault(w => string.Equals(w.Name, DataSheetName, StringComparison.OrdinalIgnoreCase))
                        ?? workbook.Worksheets.FirstOrDefault()
                        ?? throw new RequestValidationException("file", "The workbook has no sheets.");

            var lastColumn = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;
            var positions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var c = 1; c <= lastColumn; c++)
            {
                var name = sheet.Cell(1, c).GetString().Trim();
                if (name.Length > 0 && !positions.ContainsKey(name))
                    positions[name] = c;
            }

            var missing = expectedColumns.Where(c => !positions.ContainsKey(c)).ToList();
            if (missing.Count > 0)
                throw new RequestValidationException(
                    "file",
                    $"Missing column(s): {string.Join(", ", missing)}. Download the template and keep its header row.");

            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
            var rows = new List<ImportSheetRow>();

            for (var r = 2; r <= lastRow; r++)
            {
                var cells = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var hasData = false;

                foreach (var column in expectedColumns)
                {
                    var text = ReadCell(sheet.Cell(r, positions[column]));
                    cells[column] = text;
                    if (text.Length > 0)
                        hasData = true;
                }

                // Fully blank rows (often left behind by formatting) are skipped.
                if (hasData)
                    rows.Add(new ImportSheetRow(r, cells));
            }

            return new ImportSheet(rows);
        }
    }

    /// <summary>Normalises a cell to plain text the importers can parse: dates "yyyy-MM-dd", numbers invariant.</summary>
    private static string ReadCell(IXLCell cell)
    {
        if (cell.IsEmpty())
            return string.Empty;

        switch (cell.DataType)
        {
            case XLDataType.DateTime:
                var date = cell.GetDateTime();
                // A time-only cell comes back on Excel's base date (1899/1900).
                return date.Year <= 1900 ? date.ToString("HH:mm", Inv) : date.ToString("yyyy-MM-dd", Inv);

            case XLDataType.TimeSpan:
                var span = cell.GetTimeSpan();
                return $"{(int)span.TotalHours:00}:{span.Minutes:00}";

            case XLDataType.Number:
                return ((decimal)cell.GetDouble()).ToString("0.##########", Inv);

            case XLDataType.Boolean:
                return cell.GetBoolean() ? "true" : "false";

            default:
                return cell.GetString().Trim();
        }
    }
}
