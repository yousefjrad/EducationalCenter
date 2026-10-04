using EducationalCenter.Application.Common.Interfaces.Repositories;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EducationalCenter.Infrastructure.Persistence.Repositories;

internal sealed class PaymentPlanRepository(AppDbContext context) : Repository<PaymentPlan>(context), IPaymentPlanRepository
{
    private IQueryable<PaymentPlan> Graph() =>
        Set.Include(p => p.Installments).ThenInclude(i => i.Payments)
            .Include(p => p.Enrollment).ThenInclude(e => e.Student)
            .Include(p => p.Enrollment).ThenInclude(e => e.Section)
            .AsSplitQuery();

    public Task<PaymentPlan?> GetByEnrollmentIdAsync(int enrollmentId, CancellationToken ct = default) =>
        Graph().FirstOrDefaultAsync(p => p.EnrollmentId == enrollmentId, ct);

    public Task<PaymentPlan?> GetByInstallmentIdAsync(int installmentId, CancellationToken ct = default) =>
        Graph().FirstOrDefaultAsync(p => p.Installments.Any(i => i.Id == installmentId), ct);
}

internal sealed class PaymentRepository(AppDbContext context) : Repository<Payment>(context), IPaymentRepository
{
    private IQueryable<Payment> Detailed() =>
        Set.Include(p => p.Receipt)
            .Include(p => p.Installment).ThenInclude(i => i.PaymentPlan)
                .ThenInclude(pl => pl.Enrollment).ThenInclude(e => e.Student)
            .Include(p => p.Installment).ThenInclude(i => i.PaymentPlan)
                .ThenInclude(pl => pl.Enrollment).ThenInclude(e => e.Section);

    public Task<Payment?> GetWithDetailsAsync(int id, CancellationToken ct = default) =>
        Detailed().FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<Payment>> GetByEnrollmentAsync(int enrollmentId, CancellationToken ct = default) =>
        await Set.AsNoTracking()
            .Where(p => p.Installment.PaymentPlan.EnrollmentId == enrollmentId)
            .ToListAsync(ct);

    public Task<(IReadOnlyList<Payment> Items, int TotalCount)> SearchAsync(
        int? enrollmentId, int? studentId, DateOnly? from, DateOnly? to, PaymentStatus? status,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = Detailed().AsNoTracking();

        if (enrollmentId.HasValue)
            query = query.Where(p => p.Installment.PaymentPlan.EnrollmentId == enrollmentId.Value);
        if (studentId.HasValue)
            query = query.Where(p => p.Installment.PaymentPlan.Enrollment.StudentId == studentId.Value);
        if (from.HasValue)
        {
            var start = DateRanges.StartOfDay(from.Value);
            query = query.Where(p => p.PaidAt >= start);
        }
        if (to.HasValue)
        {
            var end = DateRanges.StartOfNextDay(to.Value);
            query = query.Where(p => p.PaidAt < end);
        }
        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        return query.OrderByDescending(p => p.PaidAt).ThenByDescending(p => p.Id).ToPageAsync(page, pageSize, ct);
    }

    public async Task<decimal> SumValidInSypByTrainerAsync(int trainerId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var start = DateRanges.StartOfDay(from);
        var end = DateRanges.StartOfNextDay(to);

        var total = await Set
            .Where(p => p.Status == PaymentStatus.Valid
                        && p.PaidAt >= start && p.PaidAt < end
                        && p.Installment.PaymentPlan.Enrollment.Section.TrainerId == trainerId)
            .SumAsync(p => (decimal?)p.AmountInSyp, ct);

        return total ?? 0m;
    }
}

internal sealed class ReceiptRepository(AppDbContext context) : Repository<Receipt>(context), IReceiptRepository
{
    private IQueryable<Receipt> Detailed() =>
        Set.AsNoTracking()
            .Include(r => r.Payment).ThenInclude(p => p.ReceivedByUser)
            .Include(r => r.Payment).ThenInclude(p => p.Installment).ThenInclude(i => i.PaymentPlan)
                .ThenInclude(pl => pl.Enrollment).ThenInclude(e => e.Student)
            .Include(r => r.Payment).ThenInclude(p => p.Installment).ThenInclude(i => i.PaymentPlan)
                .ThenInclude(pl => pl.Enrollment).ThenInclude(e => e.Section).ThenInclude(s => s.Course);

    public Task<Receipt?> GetWithDetailsAsync(int id, CancellationToken ct = default) =>
        Detailed().FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<Receipt?> GetByPaymentIdAsync(int paymentId, CancellationToken ct = default) =>
        Detailed().FirstOrDefaultAsync(r => r.PaymentId == paymentId, ct);
}

internal sealed class ExpenseRepository(AppDbContext context) : Repository<Expense>(context), IExpenseRepository
{
    public Task<(IReadOnlyList<Expense> Items, int TotalCount)> SearchAsync(
        string? category, DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken ct = default)
    {
        var query = Set.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(e => e.Category == category);
        if (from.HasValue)
            query = query.Where(e => e.ExpenseDate >= from.Value);
        if (to.HasValue)
            query = query.Where(e => e.ExpenseDate <= to.Value);

        return query.OrderByDescending(e => e.ExpenseDate).ThenByDescending(e => e.Id).ToPageAsync(page, pageSize, ct);
    }

    public async Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken ct = default) =>
        await Set.AsNoTracking().Select(e => e.Category).Distinct().OrderBy(c => c).ToListAsync(ct);
}

internal sealed class TrainerPayrollRepository(AppDbContext context)
    : Repository<TrainerPayroll>(context), ITrainerPayrollRepository
{
    public Task<TrainerPayroll?> GetWithDetailsAsync(int id, CancellationToken ct = default) =>
        Set.Include(p => p.Trainer).FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<bool> HasOverlapAsync(int trainerId, DateOnly start, DateOnly end, CancellationToken ct = default) =>
        Set.AnyAsync(p => p.TrainerId == trainerId
                          && p.Status != PayrollStatus.Cancelled
                          && p.PeriodStart <= end && p.PeriodEnd >= start, ct);

    public Task<(IReadOnlyList<TrainerPayroll> Items, int TotalCount)> SearchAsync(
        int? trainerId, PayrollStatus? status, DateOnly? from, DateOnly? to,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = Set.AsNoTracking().Include(p => p.Trainer).AsQueryable();

        if (trainerId.HasValue)
            query = query.Where(p => p.TrainerId == trainerId.Value);
        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);
        if (from.HasValue)
            query = query.Where(p => p.PeriodEnd >= from.Value);
        if (to.HasValue)
            query = query.Where(p => p.PeriodStart <= to.Value);

        return query.OrderByDescending(p => p.PeriodStart).ThenByDescending(p => p.Id).ToPageAsync(page, pageSize, ct);
    }
}
