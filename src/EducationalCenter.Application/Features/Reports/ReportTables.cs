using System.Globalization;
using EducationalCenter.Application.Common.Models;

namespace EducationalCenter.Application.Features.Reports;

/// <summary>Turns report data into translated, formatted tables for the Excel and PDF exporters.</summary>
internal static class ReportTables
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    private static readonly ReportSummaryLine[] NoSummary = [];

    private static string L(string lang, string ar, string en) => lang == "ar" ? ar : en;
    private static string N(decimal value) => value.ToString("N0", Invariant);
    private static string N(int value) => value.ToString("N0", Invariant);
    private static string D(DateOnly value) => value.ToString("yyyy-MM-dd", Invariant);
    private static string T(TimeOnly value) => value.ToString("HH:mm", Invariant);

    public static ReportTable MonthlyRevenue(MonthlyRevenueReportDto report, string lang) => new(
        $"{L(lang, "الإيرادات الشهرية", "Monthly revenue")} ({D(report.From)} - {D(report.To)})",
        lang,
        new[]
        {
            L(lang, "الشهر", "Month"),
            L(lang, "عدد الدفعات", "Payments"),
            L(lang, "الإجمالي (ل.س)", "Total (SYP)"),
            L(lang, "المقبوض بالليرة", "Received in SYP"),
            L(lang, "المقبوض بالدولار", "Received in USD"),
            L(lang, "مكافئ الدولار (ل.س)", "USD value (SYP)")
        },
        report.Rows.Select(r => new[]
        {
            $"{r.Year}-{r.Month:00}",
            N(r.PaymentsCount),
            N(r.TotalInSyp),
            N(r.ReceivedInSyp),
            r.ReceivedInUsd.ToString("N2", Invariant),
            N(r.UsdValueInSyp)
        }).ToList(),
        new[]
        {
            new ReportSummaryLine(L(lang, "عدد الدفعات", "Payments"), N(report.TotalPayments)),
            new ReportSummaryLine(L(lang, "إجمالي الإيرادات (ل.س)", "Total revenue (SYP)"), N(report.TotalInSyp))
        });

    public static ReportTable StudentBalances(StudentBalancesReportDto report, string lang) => new(
        L(lang, "المتبقي على الطلاب", "Student balances"),
        lang,
        new[]
        {
            L(lang, "الطالب", "Student"),
            L(lang, "الهاتف", "Phone"),
            L(lang, "المادة", "Course"),
            L(lang, "الشعبة", "Section"),
            L(lang, "السعر (ل.س)", "Price (SYP)"),
            L(lang, "المدفوع (ل.س)", "Paid (SYP)"),
            L(lang, "المتبقي (ل.س)", "Remaining (SYP)")
        },
        report.Rows.Select(r => new[]
        {
            r.StudentName, r.PhoneNumber, r.CourseName, r.SectionName,
            N(r.AgreedPrice), N(r.PaidInSyp), N(r.RemainingInSyp)
        }).ToList(),
        new[]
        {
            new ReportSummaryLine(L(lang, "إجمالي المتبقي (ل.س)", "Total remaining (SYP)"), N(report.TotalRemainingInSyp))
        });

    public static ReportTable OverdueInstallments(OverdueInstallmentsReportDto report, string lang) => new(
        $"{L(lang, "الدفعات المتأخرة", "Overdue installments")} ({D(report.AsOf)})",
        lang,
        new[]
        {
            L(lang, "الطالب", "Student"),
            L(lang, "الهاتف", "Phone"),
            L(lang, "الشعبة", "Section"),
            L(lang, "رقم الدفعة", "Installment"),
            L(lang, "تاريخ الاستحقاق", "Due date"),
            L(lang, "أيام التأخر", "Days overdue"),
            L(lang, "المبلغ (ل.س)", "Amount (SYP)"),
            L(lang, "المدفوع (ل.س)", "Paid (SYP)"),
            L(lang, "المتبقي (ل.س)", "Remaining (SYP)")
        },
        report.Rows.Select(r => new[]
        {
            r.StudentName, r.PhoneNumber, r.SectionName,
            r.InstallmentNumber.ToString(Invariant), D(r.DueDate), N(r.DaysOverdue),
            N(r.Amount), N(r.PaidInSyp), N(r.RemainingInSyp)
        }).ToList(),
        new[]
        {
            new ReportSummaryLine(L(lang, "إجمالي المتأخر (ل.س)", "Total overdue (SYP)"), N(report.TotalRemainingInSyp))
        });

    public static ReportTable NetProfit(NetProfitReportDto report, string lang)
    {
        var rows = new List<string[]>
        {
            new[] { L(lang, "الإيرادات", "Revenue"), N(report.RevenueInSyp) },
            new[] { L(lang, "المصاريف", "Expenses"), N(report.ExpensesInSyp) },
            new[] { L(lang, "رواتب المدربين", "Trainer payroll"), N(report.TrainerPayrollInSyp) },
            new[] { L(lang, "صافي الربح", "Net profit"), N(report.NetProfitInSyp) }
        };

        foreach (var category in report.ExpensesByCategory)
            rows.Add(new[] { $"{L(lang, "مصاريف", "Expenses")}: {category.Category}", N(category.TotalInSyp) });

        return new ReportTable(
            $"{L(lang, "تقرير الربح الصافي", "Net profit")} ({D(report.From)} - {D(report.To)})",
            lang,
            new[] { L(lang, "البند", "Item"), L(lang, "المبلغ (ل.س)", "Amount (SYP)") },
            rows,
            NoSummary);
    }

    public static ReportTable SectionOccupancy(SectionOccupancyReportDto report, string lang) => new(
        L(lang, "الطلاب والمقاعد الشاغرة لكل شعبة", "Students and vacant seats per section"),
        lang,
        new[]
        {
            L(lang, "الشعبة", "Section"),
            L(lang, "المادة", "Course"),
            L(lang, "المدرب", "Trainer"),
            L(lang, "القاعة", "Room"),
            L(lang, "الحالة", "Status"),
            L(lang, "السعة", "Capacity"),
            L(lang, "المسجلون", "Enrolled"),
            L(lang, "الشاغر", "Vacant"),
            L(lang, "بالانتظار", "Waiting")
        },
        report.Rows.Select(r => new[]
        {
            r.SectionName, r.CourseName, r.TrainerName, r.RoomName, r.Status.ToString(),
            N(r.Capacity), N(r.SeatsTaken), N(r.VacantSeats), N(r.WaitingCount)
        }).ToList(),
        new[]
        {
            new ReportSummaryLine(L(lang, "إجمالي السعة", "Total capacity"), N(report.TotalCapacity)),
            new ReportSummaryLine(L(lang, "إجمالي المسجلين", "Total enrolled"), N(report.TotalSeatsTaken)),
            new ReportSummaryLine(L(lang, "إجمالي الشاغر", "Total vacant"), N(report.TotalVacantSeats)),
            new ReportSummaryLine(L(lang, "إجمالي المنتظرين", "Total waiting"), N(report.TotalWaiting))
        });

    /// <param name="kindAr">"القاعة" / "المدرب" ... the owner type shown in the title.</param>
    public static ReportTable Schedule(ScheduleReportDto report, string lang, string kindAr, string kindEn) => new(
        $"{L(lang, "جدول", "Schedule")} {L(lang, kindAr, kindEn)}: {report.OwnerName} ({D(report.From)} - {D(report.To)})",
        lang,
        new[]
        {
            L(lang, "التاريخ", "Date"),
            L(lang, "من", "From"),
            L(lang, "إلى", "To"),
            L(lang, "المادة", "Course"),
            L(lang, "الشعبة", "Section"),
            L(lang, "القاعة", "Room"),
            L(lang, "المدرب", "Trainer"),
            L(lang, "الحالة", "Status")
        },
        report.Rows.Select(r => new[]
        {
            D(r.Date), T(r.StartTime), T(r.EndTime), r.CourseName, r.SectionName,
            r.RoomName, r.TrainerName, r.Status.ToString()
        }).ToList(),
        new[]
        {
            new ReportSummaryLine(L(lang, "عدد الجلسات", "Sessions"), N(report.Rows.Count))
        });
}
