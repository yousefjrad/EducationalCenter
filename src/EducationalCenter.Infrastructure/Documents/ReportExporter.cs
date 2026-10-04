using ClosedXML.Excel;
using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Common.Models;
using EducationalCenter.Domain.Exceptions;

namespace EducationalCenter.Infrastructure.Documents;

/// <summary>Turns the already translated and formatted ReportTable into an Excel file. PDF is parked (docs/PDF.md).</summary>
internal sealed class ReportExporter : IReportExporter
{
    private const int HeaderRow = 3;

    public byte[] ToExcel(ReportTable table)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Report");
        sheet.RightToLeft = table.Language == "ar";

        var columnCount = table.Columns.Count;

        var title = sheet.Cell(1, 1);
        title.Value = table.Title;
        title.Style.Font.Bold = true;
        title.Style.Font.FontSize = 14;

        for (var c = 0; c < columnCount; c++)
            sheet.Cell(HeaderRow, c + 1).Value = table.Columns[c];

        var header = sheet.Range(HeaderRow, 1, HeaderRow, columnCount);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.LightGray;

        var rowIndex = HeaderRow + 1;
        foreach (var row in table.Rows)
        {
            for (var c = 0; c < columnCount && c < row.Count; c++)
                sheet.Cell(rowIndex, c + 1).Value = row[c];
            rowIndex++;
        }

        if (table.Summary.Count > 0)
        {
            rowIndex++;
            foreach (var line in table.Summary)
            {
                var label = sheet.Cell(rowIndex, 1);
                label.Value = line.Label;
                label.Style.Font.Bold = true;
                sheet.Cell(rowIndex, 2).Value = line.Value;
                rowIndex++;
            }
        }

        // The title sits in column A, so it is left out when sizing columns.
        sheet.Columns(1, columnCount).AdjustToContents(HeaderRow, rowIndex);
        sheet.SheetView.FreezeRows(HeaderRow);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ToPdf(ReportTable table) =>
        throw new FeatureUnavailableException("PDF export is not installed in this version yet. Excel export is available.");
}
