using System.Globalization;
using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Common.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace EducationalCenter.Infrastructure.Documents;

internal sealed class QuestPdfCertificateGenerator : ICertificatePdfGenerator
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private static readonly string[] LogoExtensions = [".png", ".jpg", ".jpeg"];

    public QuestPdfCertificateGenerator() => PdfSetup.EnsureInitialized();

    public byte[] Generate(CertificatePdfModel model)
    {
        var rtl = model.Language == "ar";
        string L(string ar, string en) => rtl ? ar : en;

        var hasLogo = !string.IsNullOrWhiteSpace(model.LogoPath)
                      && LogoExtensions.Contains(Path.GetExtension(model.LogoPath), StringComparer.OrdinalIgnoreCase)
                      && File.Exists(model.LogoPath);

        var date = model.IssueDate.ToString("yyyy-MM-dd", Inv);

        return Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(30);
            if (rtl)
                page.ContentFromRightToLeft();
            page.DefaultTextStyle(t => t.FontFamily(PdfSetup.FontFamily));

            page.Content().Border(4).BorderColor(Colors.Blue.Darken3).Padding(30).Column(col =>
            {
                col.Spacing(14);

                if (hasLogo)
                    col.Item().AlignCenter().Height(70).Image(model.LogoPath!).FitArea();

                col.Item().AlignCenter().Text(model.CenterName).FontSize(26).Bold();

                col.Item().AlignCenter().Text(L("شهادة إتمام", "Certificate of Completion"))
                    .FontSize(32).Bold().FontColor(Colors.Blue.Darken3);

                col.Item().PaddingVertical(20).Text(text =>
                {
                    text.AlignCenter();
                    text.Span(model.Body).FontSize(20).LineHeight(1.5f);
                });

                col.Item().PaddingTop(30).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().AlignCenter().Text($"{L("رقم الشهادة", "Certificate no.")}: {model.CertificateNumber}");
                        c.Item().AlignCenter().Text($"{L("التاريخ", "Date")}: {date}");
                    });

                    row.RelativeItem().Column(c =>
                    {
                        c.Item().PaddingTop(20).LineHorizontal(1);
                        c.Item().AlignCenter().Text(model.SignerName).Bold();
                        c.Item().AlignCenter().Text(model.SignerTitle);
                    });
                });
            });
        })).GeneratePdf();
    }
}
