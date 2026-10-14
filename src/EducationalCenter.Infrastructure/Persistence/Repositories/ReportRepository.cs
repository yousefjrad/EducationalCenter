using EducationalCenter.Application.Common.Interfaces.Repositories;
using EducationalCenter.Application.Features.Alerts;
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

    public async Task<IReadOnlyList<BranchComparisonRow>> GetBranchComparisonAsync(
        DateOnly from, DateOnly to, DateTime utcNow, DateOnly today, CancellationToken ct = default)
    {
        var start = DateRanges.StartOfDay(from);
        var end = DateRanges.StartOfNextDay(to);

        var branches = await db.Branches.AsNoTracking()
            .OrderBy(b => b.Name)
            .Select(b => new { b.Id, b.Name, b.Code, b.IsActive })
            .ToListAsync(ct);

        var revenue = (await db.Payments.IgnoreQueryFilters().AsNoTracking()
            .Where(p => !p.IsDeleted && p.Status == PaymentStatus.Valid && p.PaidAt >= start && p.PaidAt < end)
            .GroupBy(p => p.BranchId)
            .Select(g => new { BranchId = g.Key, Total = g.Sum(p => p.AmountInSyp) })
            .ToListAsync(ct)).ToDictionary(x => x.BranchId, x => x.Total);

        var expenses = (await db.Expenses.IgnoreQueryFilters().AsNoTracking()
            .Where(e => !e.IsDeleted && e.ExpenseDate >= from && e.ExpenseDate <= to)
            .GroupBy(e => e.BranchId)
            .Select(g => new { BranchId = g.Key, Total = g.Sum(e => e.AmountInSyp) })
            .ToListAsync(ct)).ToDictionary(x => x.BranchId, x => x.Total);

        var payroll = (await db.TrainerPayrolls.IgnoreQueryFilters().AsNoTracking()
            .Where(p => !p.IsDeleted && p.Status == PayrollStatus.Paid && p.PaidAt >= start && p.PaidAt < end)
            .GroupBy(p => p.BranchId)
            .Select(g => new { BranchId = g.Key, Total = g.Sum(p => p.AmountInSyp) })
            .ToListAsync(ct)).ToDictionary(x => x.BranchId, x => x.Total);

        var students = (await db.Students.IgnoreQueryFilters().AsNoTracking()
            .Where(s => !s.IsDeleted && s.IsActive)
            .GroupBy(s => s.BranchId)
            .Select(g => new { BranchId = g.Key, Count = g.Count() })
            .ToListAsync(ct)).ToDictionary(x => x.BranchId, x => x.Count);

        var newEnrollments = (await db.Enrollments.IgnoreQueryFilters().AsNoTracking()
            .Where(e => !e.IsDeleted && e.EnrolledAt >= start && e.EnrolledAt < end
                        && (e.Status == EnrollmentStatus.Confirmed || e.Status == EnrollmentStatus.Completed))
            .GroupBy(e => e.BranchId)
            .Select(g => new { BranchId = g.Key, Count = g.Count() })
            .ToListAsync(ct)).ToDictionary(x => x.BranchId, x => x.Count);

        var attendance = (await db.Attendances.IgnoreQueryFilters().AsNoTracking()
            .Where(a => !a.IsDeleted && !a.ClassSession.IsDeleted
                        && a.ClassSession.Date >= from && a.ClassSession.Date <= to)
            .GroupBy(a => a.BranchId)
            .Select(g => new { BranchId = g.Key, Total = g.Count(), Present = g.Count(a => a.IsPresent) })
            .ToListAsync(ct)).ToDictionary(x => x.BranchId);

        var capacity = (await db.Sections.IgnoreQueryFilters().AsNoTracking()
            .Where(s => !s.IsDeleted
                        && (s.Status == SectionStatus.OpenForEnrollment || s.Status == SectionStatus.InProgress))
            .GroupBy(s => s.BranchId)
            .Select(g => new { BranchId = g.Key, Total = g.Sum(s => s.Capacity) })
            .ToListAsync(ct)).ToDictionary(x => x.BranchId, x => x.Total);

        var seatsTaken = (await db.Enrollments.IgnoreQueryFilters().AsNoTracking()
            .Where(e => !e.IsDeleted && !e.Section.IsDeleted
                        && (e.Section.Status == SectionStatus.OpenForEnrollment || e.Section.Status == SectionStatus.InProgress)
                        && (e.Status == EnrollmentStatus.Confirmed
                            || (e.Status == EnrollmentStatus.Pending && e.HoldExpiresAt > utcNow)))
            .GroupBy(e => e.BranchId)
            .Select(g => new { BranchId = g.Key, Count = g.Count() })
            .ToListAsync(ct)).ToDictionary(x => x.BranchId, x => x.Count);

        var installments = await db.Installments.IgnoreQueryFilters().AsNoTracking()
            .Where(i => !i.IsDeleted && !i.PaymentPlan.IsDeleted
                        && i.PaymentPlan.Status != PaymentPlanStatus.Cancelled
                        && ((i.DueDate >= from && i.DueDate <= to)
                            || (i.DueDate < today && i.PaymentPlan.Status == PaymentPlanStatus.Open
                                && i.Status != InstallmentStatus.Paid)))
            .Select(i => new
            {
                i.BranchId,
                i.Amount,
                i.DueDate,
                PlanOpen = i.PaymentPlan.Status == PaymentPlanStatus.Open,
                InstallmentStatus = i.Status,
                Paid = i.Payments
                    .Where(p => !p.IsDeleted && p.Status == PaymentStatus.Valid)
                    .Sum(p => (decimal?)p.AmountInSyp) ?? 0m
            })
            .ToListAsync(ct);

        static decimal? Pct(decimal part, decimal whole) => whole > 0m ? Math.Round(part * 100m / whole, 1) : null;

        return branches.Select(b =>
        {
            var rev = revenue.GetValueOrDefault(b.Id);
            var exp = expenses.GetValueOrDefault(b.Id);
            var pay = payroll.GetValueOrDefault(b.Id);

            decimal? attendanceRate = attendance.TryGetValue(b.Id, out var att) ? Pct(att.Present, att.Total) : null;
            var fillRate = Pct(seatsTaken.GetValueOrDefault(b.Id), capacity.GetValueOrDefault(b.Id));

            var mine = installments.Where(i => i.BranchId == b.Id).ToList();
            var inPeriod = mine.Where(i => i.DueDate >= from && i.DueDate <= to).ToList();
            var due = inPeriod.Sum(i => i.Amount);
            var collected = inPeriod.Sum(i => Math.Min(i.Amount, i.Paid));

            var overdue = mine
                .Where(i => i.PlanOpen && i.InstallmentStatus != InstallmentStatus.Paid && i.DueDate < today && i.Amount > i.Paid)
                .ToList();

            return new BranchComparisonRow(
                b.Id, b.Name, b.Code, b.IsActive,
                rev, exp, pay, rev - exp - pay,
                students.GetValueOrDefault(b.Id),
                newEnrollments.GetValueOrDefault(b.Id),
                attendanceRate, fillRate, Pct(collected, due),
                overdue.Sum(i => i.Amount - i.Paid),
                overdue.Count);
        }).ToList();
    }

    public async Task<IReadOnlyList<WaitingPromotionRow>> GetPromotableWaitingAsync(
        DateTime utcNow, CancellationToken ct = default)
    {
        var sections = await db.Sections.AsNoTracking()
            .Where(s => (s.Status == SectionStatus.OpenForEnrollment || s.Status == SectionStatus.InProgress)
                        && s.WaitingList.Any(w => w.Status == WaitingListStatus.Waiting))
            .Select(s => new
            {
                s.Id,
                s.Name,
                CourseName = s.Course.Name,
                s.Capacity,
                SeatsTaken = s.Enrollments.Count(e => e.Status == EnrollmentStatus.Confirmed
                                                      || (e.Status == EnrollmentStatus.Pending && e.HoldExpiresAt > utcNow)),
                WaitingCount = s.WaitingList.Count(w => w.Status == WaitingListStatus.Waiting)
            })
            .ToListAsync(ct);

        var withVacancy = sections.Where(s => s.SeatsTaken < s.Capacity).ToList();
        if (withVacancy.Count == 0)
            return [];

        // Second, small query: the people waiting in just those sections, so the first in line can be named.
        var sectionIds = withVacancy.Select(s => s.Id).ToList();
        var waiting = await db.WaitingListEntries.AsNoTracking()
            .Include(w => w.Student)
            .Where(w => sectionIds.Contains(w.SectionId) && w.Status == WaitingListStatus.Waiting)
            .OrderBy(w => w.SectionId).ThenBy(w => w.Position)
            .ToListAsync(ct);

        var firstBySection = waiting.GroupBy(w => w.SectionId).ToDictionary(g => g.Key, g => g.First());

        return withVacancy
            .Where(s => firstBySection.ContainsKey(s.Id))
            .Select(s =>
            {
                var first = firstBySection[s.Id];
                return new WaitingPromotionRow(
                    s.Id, s.Name, s.CourseName, s.Capacity - s.SeatsTaken, s.WaitingCount,
                    first.Id, first.StudentId, first.Student.FullName, first.Student.PhoneNumber);
            })
            .OrderBy(r => r.CourseName).ThenBy(r => r.SectionName)
            .ToList();
    }
}
