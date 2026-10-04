using EducationalCenter.Application.Common.Interfaces.Repositories;
using EducationalCenter.Application.Features.Reports;
using EducationalCenter.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EducationalCenter.Infrastructure.Persistence.Repositories;

/// <summary>Read-only aggregate queries. The sums and groupings run in SQL Server.</summary>
internal sealed class ReportRepository(AppDbContext db) : IReportRepository
{
    public async Task<IReadOnlyList<MonthlyRevenueRow>> GetMonthlyRevenueAsync(
        DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var start = DateRanges.StartOfDay(from);
        var end = DateRanges.StartOfNextDay(to);

        var groups = await db.Payments.AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Valid && p.PaidAt >= start && p.PaidAt < end)
            .GroupBy(p => new { p.PaidAt.Year, p.PaidAt.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Count = g.Count(),
                Total = g.Sum(p => p.AmountInSyp),
                ReceivedInSyp = g.Where(p => p.Currency == Currency.Syp).Sum(p => p.AmountInSyp),
                ReceivedInUsd = g.Where(p => p.Currency == Currency.Usd).Sum(p => p.AmountPaid),
                UsdValueInSyp = g.Where(p => p.Currency == Currency.Usd).Sum(p => p.AmountInSyp)
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync(ct);

        return groups
            .Select(x => new MonthlyRevenueRow(
                x.Year, x.Month, x.Count, x.Total, x.ReceivedInSyp, x.ReceivedInUsd, x.UsdValueInSyp))
            .ToList();
    }

    public async Task<IReadOnlyList<StudentBalanceRow>> GetStudentBalancesAsync(
        int? sectionId, int? courseId, CancellationToken ct = default)
    {
        var query = db.Enrollments.AsNoTracking()
            .Where(e => e.Status == EnrollmentStatus.Confirmed || e.Status == EnrollmentStatus.Completed);

        if (sectionId.HasValue)
            query = query.Where(e => e.SectionId == sectionId.Value);
        if (courseId.HasValue)
            query = query.Where(e => e.Section.CourseId == courseId.Value);

        // One query: each enrollment with the sum of its valid payments computed in SQL.
        var rows = await query
            .Select(e => new
            {
                EnrollmentId = e.Id,
                e.StudentId,
                StudentName = e.Student.FullName,
                e.Student.PhoneNumber,
                e.SectionId,
                SectionName = e.Section.Name,
                CourseName = e.Section.Course.Name,
                e.AgreedPrice,
                Paid = db.Payments
                    .Where(p => p.Status == PaymentStatus.Valid && p.Installment.PaymentPlan.EnrollmentId == e.Id)
                    .Sum(p => (decimal?)p.AmountInSyp) ?? 0m
            })
            .ToListAsync(ct);

        return rows
            .Where(r => r.AgreedPrice > r.Paid)
            .Select(r => new StudentBalanceRow(
                r.EnrollmentId, r.StudentId, r.StudentName, r.PhoneNumber, r.SectionId, r.SectionName,
                r.CourseName, r.AgreedPrice, r.Paid, r.AgreedPrice - r.Paid))
            .OrderByDescending(r => r.RemainingInSyp).ThenBy(r => r.StudentName)
            .ToList();
    }

    public async Task<IReadOnlyList<OverdueInstallmentRow>> GetOverdueInstallmentsAsync(
        DateOnly asOf, int? sectionId, CancellationToken ct = default)
    {
        var query = db.Installments.AsNoTracking()
            .Where(i => i.PaymentPlan.Status == PaymentPlanStatus.Open
                        && i.Status != InstallmentStatus.Paid
                        && i.DueDate < asOf);

        if (sectionId.HasValue)
            query = query.Where(i => i.PaymentPlan.Enrollment.SectionId == sectionId.Value);

        var rows = await query
            .Select(i => new
            {
                InstallmentId = i.Id,
                i.PaymentPlan.EnrollmentId,
                i.PaymentPlan.Enrollment.StudentId,
                StudentName = i.PaymentPlan.Enrollment.Student.FullName,
                i.PaymentPlan.Enrollment.Student.PhoneNumber,
                SectionName = i.PaymentPlan.Enrollment.Section.Name,
                i.Number,
                i.DueDate,
                i.Amount,
                Paid = i.Payments
                    .Where(p => p.Status == PaymentStatus.Valid)
                    .Sum(p => (decimal?)p.AmountInSyp) ?? 0m
            })
            .ToListAsync(ct);

        // DaysOverdue stays 0 here; the service fills it in.
        return rows
            .Where(r => r.Amount > r.Paid)
            .Select(r => new OverdueInstallmentRow(
                r.InstallmentId, r.EnrollmentId, r.StudentId, r.StudentName, r.PhoneNumber, r.SectionName,
                r.Number, r.DueDate, 0, r.Amount, r.Paid, r.Amount - r.Paid))
            .OrderBy(r => r.DueDate).ThenBy(r => r.StudentName)
            .ToList();
    }

    public async Task<NetProfitFigures> GetNetProfitFiguresAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var start = DateRanges.StartOfDay(from);
        var end = DateRanges.StartOfNextDay(to);

        var revenue = await db.Payments.AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Valid && p.PaidAt >= start && p.PaidAt < end)
            .SumAsync(p => (decimal?)p.AmountInSyp, ct);

        var expensesInRange = db.Expenses.AsNoTracking()
            .Where(e => e.ExpenseDate >= from && e.ExpenseDate <= to);

        var expenses = await expensesInRange.SumAsync(e => (decimal?)e.AmountInSyp, ct);

        var byCategory = await expensesInRange
            .GroupBy(e => e.Category)
            .Select(g => new { Category = g.Key, Total = g.Sum(e => e.AmountInSyp) })
            .OrderByDescending(x => x.Total)
            .ToListAsync(ct);

        var payroll = await db.TrainerPayrolls.AsNoTracking()
            .Where(p => p.Status == PayrollStatus.Paid && p.PaidAt >= start && p.PaidAt < end)
            .SumAsync(p => (decimal?)p.AmountInSyp, ct);

        return new NetProfitFigures(
            revenue ?? 0m,
            expenses ?? 0m,
            payroll ?? 0m,
            byCategory.Select(x => new CategoryTotal(x.Category, x.Total)).ToList());
    }

    public async Task<IReadOnlyList<SectionOccupancyRow>> GetSectionOccupancyAsync(
        DateTime utcNow, SectionStatus? status, int? courseId, CancellationToken ct = default)
    {
        var query = db.Sections.AsNoTracking().AsQueryable();

        query = status.HasValue
            ? query.Where(s => s.Status == status.Value)
            : query.Where(s => s.Status == SectionStatus.Draft
                               || s.Status == SectionStatus.OpenForEnrollment
                               || s.Status == SectionStatus.InProgress);

        if (courseId.HasValue)
            query = query.Where(s => s.CourseId == courseId.Value);

        return await query
            .OrderBy(s => s.Course.Name).ThenBy(s => s.Name)
            .Select(s => new SectionOccupancyRow(
                s.Id,
                s.Name,
                s.Course.Name,
                s.Trainer.FullName,
                s.Room.Name,
                s.Status,
                s.Capacity,
                s.Enrollments.Count(e => e.Status == EnrollmentStatus.Confirmed
                                         || (e.Status == EnrollmentStatus.Pending && e.HoldExpiresAt > utcNow)),
                s.WaitingList.Count(w => w.Status == WaitingListStatus.Waiting)))
            .ToListAsync(ct);
    }
}
