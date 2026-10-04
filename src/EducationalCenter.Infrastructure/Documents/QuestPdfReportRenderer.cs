using EducationalCenter.Application.Common.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace EducationalCenter.Infrastructure.Documents;

/// <summary>PARKED (not compiled): see docs/PDF.md. ReportExporter.ToPdf calls this once PDF is switched back on.</summary>
internal static class QuestPdfReportRenderer
{
    public static byte[] Render(ReportTable table)
    {
        PdfSetup.EnsureInitialized();

        var rtl = table.Language == "ar";

        return Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(25);
            if (rtl)
                page.ContentFromRightToLeft();
            page.DefaultTextStyle(t => t.FontFamily(PdfSetup.FontFamily).FontSize(9));

            page.Header().Text(table.Title).FontSize(15).Bold();

            page.Content().PaddingVertical(8).Column(col =>
            {
                if (table.Rows.Count == 0)
                {
                    col.Item().Text(rtl ? "لا توجد بيانات" : "No data");
                }
                else
                {
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            foreach (var _ in table.Columns)
                                c.RelativeColumn();
                        });

                        t.Header(h =>
                        {
                            foreach (var column in table.Columns)
                                h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(column).Bold();
                        });

                        foreach (var row in table.Rows)
                        {
                            for (var c = 0; c < table.Columns.Count; c++)
                            {
                                var text = c < row.Count ? row[c] : string.Empty;
                                t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(text);
                            }
                        }
                    });
                }

                if (table.Summary.Count > 0)
                {
                    col.Item().PaddingTop(12).Column(summary =>
                    {
                        foreach (var line in table.Summary)
                        {
                            summary.Item().Text(text =>
                            {
                                text.Span($"{line.Label}: ").Bold();
                                text.Span(line.Value);
                            });
                        }
                    });
                }
            });

            page.Footer().AlignCenter().Text(text =>
            {
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
        })).GeneratePdf();
    }
}
