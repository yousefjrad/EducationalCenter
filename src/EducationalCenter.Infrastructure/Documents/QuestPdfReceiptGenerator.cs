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

        var paid = model.Currency == Currency.Usd
            ? $"{model.AmountPaid.ToString("N2", Inv)} USD"
            : $"{model.AmountPaid.ToString("N0", Inv)} SYP";

        var rows = new List<(string Label, string Value)>
        {
            (L("رقم الإيصال", "Receipt no."), model.ReceiptNumber),
            (L("التاريخ", "Date"), issued),
            (L("الطالب", "Student"), model.StudentName),
            (L("المادة", "Course"), model.CourseName),
            (L("الشعبة", "Section"), model.SectionName),
            (L("الدفعة رقم", "Installment no."), model.InstallmentNumber.ToString(Inv)),
            (L("المبلغ المدفوع", "Amount paid"), paid)
        };

        if (model.Currency == Currency.Usd)
        {
            rows.Add((L("سعر الصرف (ل.س لكل دولار)", "Exchange rate (SYP per USD)"),
                model.ExchangeRate?.ToString("#,##0.####", Inv) ?? "-"));
            rows.Add((L("المكافئ بالليرة", "Equivalent in SYP"), $"{model.AmountInSyp.ToString("N0", Inv)} SYP"));
        }

        rows.Add((L("المتبقي بعد هذه الدفعة", "Balance after this payment"), $"{model.BalanceAfterInSyp.ToString("N0", Inv)} SYP"));
        rows.Add((L("استلمها", "Received by"), model.ReceivedByName));

        return Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A5.Landscape());
            page.Margin(24);
            if (rtl)
                page.ContentFromRightToLeft();
            page.DefaultTextStyle(t => t.FontFamily(PdfSetup.FontFamily).FontSize(11));

            page.Header().Column(col =>
            {
                col.Item().AlignCenter().Text(model.CenterName).FontSize(18).Bold();
                col.Item().AlignCenter().Text(L("إيصال استلام", "Payment receipt")).FontSize(14);

                if (model.IsVoid)
                    col.Item().AlignCenter().Text(L("ملغى", "VOID")).FontSize(20).Bold().FontColor(Colors.Red.Medium);

                col.Item().PaddingTop(6).LineHorizontal(1);
            });

            page.Content().PaddingVertical(10).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(2);
                    c.RelativeColumn(3);
                });

                foreach (var (label, value) in rows)
                {
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(label).Bold();
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(value);
                }
            });
        })).GeneratePdf();
    }
}
