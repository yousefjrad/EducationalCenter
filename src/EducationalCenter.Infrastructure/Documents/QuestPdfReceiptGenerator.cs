using System.Globalization;
using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Common.Models;
using EducationalCenter.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace EducationalCenter.Infrastructure.Documents;

internal sealed class QuestPdfReceiptGenerator : IReceiptPdfGenerator
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public QuestPdfReceiptGenerator() => PdfSetup.EnsureInitialized();

    public byte[] Generate(ReceiptPdfModel model)
    {
        var rtl = model.Language == "ar";
        string L(string ar, string en) => rtl ? ar : en;

        // Stored as UTC; the receipt shows the clock of the machine the center runs on.
        var issued = model.IssuedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm", Inv);
        var printed = DateTime.Now.ToString("yyyy-MM-dd HH:mm", Inv);

        var paid = model.Currency == Currency.Usd
            ? $"{model.AmountPaid.ToString("N2", Inv)} USD"
            : $"{model.AmountPaid.ToString("N0", Inv)} SYP";

        var balance = $"{model.BalanceAfterInSyp.ToString("N0", Inv)} SYP";

        var rows = new List<(string Label, string Value)>
        {
            (L(PdfText.Date, "Date"), issued),
            (L(PdfText.Student, "Student"), model.StudentName),
            (L(PdfText.Course, "Course"), model.CourseName),
            (L(PdfText.Section, "Section"), model.SectionName),
            (L(PdfText.InstallmentNumber, "Installment no."), model.InstallmentNumber.ToString(Inv))
        };

        if (model.Currency == Currency.Usd)
        {
            rows.Add((L(PdfText.ExchangeRate, "Exchange rate (SYP per USD)"),
                model.ExchangeRate?.ToString("#,##0.####", Inv) ?? "-"));
            rows.Add((L(PdfText.EquivalentInSyp, "Equivalent in SYP"), $"{model.AmountInSyp.ToString("N0", Inv)} SYP"));
        }

        return Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A5);
            page.Margin(0);
            if (rtl)
                page.ContentFromRightToLeft();
            page.DefaultTextStyle(t => t.FontFamily(PdfSetup.FontFamily).FontSize(11).FontColor(PdfTheme.Ink));

            page.Header().Column(head =>
            {
                head.Item().Background(PdfTheme.Navy).PaddingVertical(16).PaddingHorizontal(24).Row(row =>
                {
                    row.RelativeItem().AlignMiddle().Column(title =>
                    {
                        title.Item().Text(model.CenterName).FontSize(17).Bold().FontColor(PdfTheme.White);
                        title.Item().Text(L(PdfText.ReceiptTitle, "Payment receipt")).FontSize(12).FontColor(PdfTheme.Gold);
                    });

                    row.AutoItem().AlignMiddle().Background(PdfTheme.White).Padding(8).Column(num =>
                    {
                        num.Item().AlignCenter().Text(L(PdfText.ReceiptNumber, "Receipt no.")).FontSize(8).FontColor(PdfTheme.Muted);
                        num.Item().AlignCenter().Text(model.ReceiptNumber).FontSize(13).Bold().FontColor(PdfTheme.Navy);
                    });
                });
                head.Item().Height(3).Background(PdfTheme.Gold);
            });

            page.Content().PaddingHorizontal(24).PaddingVertical(14).Column(col =>
            {
                col.Spacing(12);

                if (model.IsVoid)
                    col.Item().Background(PdfTheme.Danger).Padding(6).AlignCenter()
                        .Text(L(PdfText.Cancelled, "VOID")).FontSize(15).Bold().FontColor(PdfTheme.White);

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(def =>
                    {
                        def.RelativeColumn(2);
                        def.RelativeColumn(3);
                    });

                    for (var i = 0; i < rows.Count; i++)
                    {
                        var alt = i % 2 == 1;
                        var (label, value) = rows[i];
                        table.Cell().Element(cell => PdfTheme.BodyCell(cell, alt)).Text(label).Bold().FontColor(PdfTheme.Navy);
                        table.Cell().Element(cell => PdfTheme.BodyCell(cell, alt)).Text(value);
                    }
                });

                col.Item().Row(row =>
                {
                    row.Spacing(10);

                    row.RelativeItem().Border(1).BorderColor(PdfTheme.Gold).Background(PdfTheme.Soft).Padding(10).Column(box =>
                    {
                        box.Item().Text(L(PdfText.AmountPaid, "Amount paid")).FontSize(9).FontColor(PdfTheme.Muted);
                        box.Item().Text(paid).FontSize(16).Bold().FontColor(PdfTheme.Navy);
                    });

                    row.RelativeItem().Border(1).BorderColor(PdfTheme.Line).Background(PdfTheme.Soft).Padding(10).Column(box =>
                    {
                        box.Item().Text(L(PdfText.BalanceAfter, "Balance after this payment")).FontSize(9).FontColor(PdfTheme.Muted);
                        box.Item().Text(balance).FontSize(16).Bold().FontColor(PdfTheme.Ink);
                    });
                });

                col.Item().PaddingTop(4).Row(row =>
                {
                    row.Spacing(10);

                    row.RelativeItem().AlignMiddle().Column(who =>
                    {
                        who.Item().Text(L(PdfText.ReceivedBy, "Received by")).FontSize(9).FontColor(PdfTheme.Muted);
                        who.Item().Text(model.ReceivedByName).FontSize(13).Bold().FontColor(PdfTheme.Navy);
                    });

                    row.RelativeItem().Height(62).Border(0.75f).BorderColor(PdfTheme.Line).Padding(4)
                        .AlignBottom().AlignCenter()
                        .Text(L(PdfText.StampAndSignature, "Stamp and signature")).FontSize(8).FontColor(PdfTheme.Muted);
                });
            });

            page.Footer().Column(foot =>
            {
                foot.Item().Height(3).Background(PdfTheme.Gold);
                foot.Item().Background(PdfTheme.Navy).Padding(8).AlignCenter()
                    .Text($"{model.CenterName}   |   {L(PdfText.Printed, "Printed")}: {printed}")
                    .FontSize(8).FontColor(PdfTheme.White);
            });
        })).GeneratePdf();
    }
}