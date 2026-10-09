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
            page.Margin(20);
            if (rtl)
                page.ContentFromRightToLeft();
            page.DefaultTextStyle(t => t.FontFamily(PdfSetup.FontFamily).FontColor(PdfTheme.Ink));

            page.Content().ExtendVertical()
                .Border(3).BorderColor(PdfTheme.Navy).Padding(5)
                .Border(1).BorderColor(PdfTheme.Gold).PaddingVertical(22).PaddingHorizontal(40)
                .Column(col =>
                {
                    col.Spacing(10);

                    if (hasLogo)
                        col.Item().AlignCenter().Height(60).Image(model.LogoPath!).FitArea();

                    col.Item().AlignCenter().Text(model.CenterName).FontSize(22).Bold().FontColor(PdfTheme.Navy);
                    col.Item().AlignCenter().Width(120).Height(2).Background(PdfTheme.Gold);

                    col.Item().PaddingTop(8).AlignCenter()
                        .Text(L(PdfText.CertificateTitle, "Certificate of Completion"))
                        .FontSize(38).Bold().FontColor(PdfTheme.Navy);

                    col.Item().PaddingVertical(14).Text(text =>
                    {
                        text.AlignCenter();
                        text.Span(model.Body).FontSize(19).LineHeight(1.6f);
                    });

                    col.Item().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem().AlignMiddle().Column(info =>
                        {
                            info.Spacing(4);
                            info.Item().AlignCenter().Text($"{L(PdfText.CertificateNumber, "Certificate no.")}: {model.CertificateNumber}")
                                .FontSize(11).FontColor(PdfTheme.Muted);
                            info.Item().AlignCenter().Text($"{L(PdfText.Date, "Date")}: {date}")
                                .FontSize(11).FontColor(PdfTheme.Muted);
                        });

                        row.RelativeItem().AlignMiddle().AlignCenter().Width(80).Height(80)
                            .Border(2).BorderColor(PdfTheme.Gold).CornerRadius(40)
                            .Padding(6).AlignMiddle().AlignCenter()
                            .Text(L(PdfText.Approved, "APPROVED")).FontSize(12).Bold().FontColor(PdfTheme.Gold);

                        row.RelativeItem().AlignMiddle().Column(sign =>
                        {
                            sign.Spacing(4);
                            sign.Item().Height(34);
                            sign.Item().Height(1).Background(PdfTheme.Navy);
                            sign.Item().AlignCenter().Text(model.SignerName).Bold().FontColor(PdfTheme.Navy);
                            sign.Item().AlignCenter().Text(model.SignerTitle).FontSize(10).FontColor(PdfTheme.Muted);
                        });
                    });
                });
        })).GeneratePdf();
    }
}