using System.Globalization;
using EducationalCenter.Application.Common.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace EducationalCenter.Infrastructure.Documents;

internal static class QuestPdfReportRenderer
{
    public static byte[] Render(ReportTable table)
    {
        PdfSetup.EnsureInitialized();

        var rtl = table.Language == "ar";
        var printed = DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        return Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(25);
            if (rtl)
                page.ContentFromRightToLeft();
            page.DefaultTextStyle(t => t.FontFamily(PdfSetup.FontFamily).FontSize(9).FontColor(PdfTheme.Ink));

            page.Header().Column(head =>
            {
                head.Item().Background(PdfTheme.Navy).Padding(12).Row(row =>
                {
                    row.RelativeItem().AlignMiddle().Text(table.Title).FontSize(16).Bold().FontColor(PdfTheme.White);
                    row.AutoItem().AlignMiddle().Text(printed).FontSize(9).FontColor(PdfTheme.Gold);
                });
                head.Item().Height(3).Background(PdfTheme.Gold);
            });

            page.Content().PaddingVertical(10).Column(col =>
            {
                col.Spacing(12);

                if (table.Rows.Count == 0)
                {
                    col.Item().AlignCenter().Text(rtl ? PdfText.NoData : "No data").FontColor(PdfTheme.Muted);
                }
                else
                {
                    col.Item().Table(grid =>
                    {
                        grid.ColumnsDefinition(def =>
                        {
                            foreach (var _ in table.Columns)
                                def.RelativeColumn();
                        });

                        grid.Header(h =>
                        {
                            foreach (var column in table.Columns)
                                h.Cell().Element(cell => PdfTheme.HeadCell(cell)).Text(column).Bold().FontColor(PdfTheme.White);
                        });

                        for (var r = 0; r < table.Rows.Count; r++)
                        {
                            var alt = r % 2 == 1;
                            var row = table.Rows[r];
                            for (var k = 0; k < table.Columns.Count; k++)
                            {
                                var value = k < row.Count ? row[k] : string.Empty;
                                grid.Cell().Element(cell => PdfTheme.BodyCell(cell, alt)).Text(value);
                            }
                        }
                    });
                }

                if (table.Summary.Count > 0)
                {
                    col.Item().Border(1).BorderColor(PdfTheme.Gold).Background(PdfTheme.Soft).Padding(10).Column(summary =>
                    {
                        summary.Spacing(3);
                        foreach (var line in table.Summary)
                        {
                            summary.Item().Text(text =>
                            {
                                text.Span($"{line.Label}: ").Bold().FontColor(PdfTheme.Navy);
                                text.Span(line.Value);
                            });
                        }
                    });
                }
            });

            page.Footer().Column(foot =>
            {
                foot.Item().Height(1).Background(PdfTheme.Line);
                foot.Item().PaddingTop(5).AlignCenter().Text(text =>
                {
                    text.DefaultTextStyle(s => s.FontSize(8).FontColor(PdfTheme.Muted));
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        })).GeneratePdf();
    }
}