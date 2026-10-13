using EducationalCenter.Application.Common.Interfaces.Repositories;
using EducationalCenter.Application.Features.Budgets;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EducationalCenter.Infrastructure.Persistence.Repositories;

internal sealed class BudgetRepository(AppDbContext context) : Repository<Budget>(context), IBudgetRepository
{
    public Task<Budget?> GetWithDetailsAsync(int id, CancellationToken ct = default) =>
        Set.Include(b => b.Branch).Include(b => b.RequestedByUser).FirstOrDefaultAsync(b => b.Id == id, ct);

    public Task<bool> ExistsAsync(DateOnly month, int branchId, int? excludeId, CancellationToken ct = default) =>
        Set.AnyAsync(b => b.BranchId == branchId && b.Month == month && (excludeId == null || b.Id != excludeId), ct);

    public Task<(IReadOnlyList<Budget> Items, int TotalCount)> SearchAsync(
        BudgetStatus? status, DateOnly? from, DateOnly? to, int? branchId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = Set.AsNoTracking().Include(b => b.Branch).Include(b => b.RequestedByUser).AsQueryable();

        if (status.HasValue)
            query = query.Where(b => b.Status == status.Value);
        if (from.HasValue)
            query = query.Where(b => b.Month >= from.Value);
        if (to.HasValue)
            query = query.Where(b => b.Month <= to.Value);
        if (branchId.HasValue)
            query = query.Where(b => b.BranchId == branchId.Value);

        return query.OrderByDescending(b => b.Month).ThenBy(b => b.Branch.Name).ThenBy(b => b.Id).ToPageAsync(page, pageSize, ct);
    }

    public async Task<decimal> GetSpentSypAsync(int branchId, DateOnly month, CancellationToken ct = default)
    {
        var next = month.AddMonths(1);

        var total = await Db.Expenses
            .Where(e => e.BranchId == branchId && e.ExpenseDate >= month && e.ExpenseDate < next)
            .SumAsync(e => (decimal?)e.AmountInSyp, ct);

        return total ?? 0m;
    }

    public async Task<IReadOnlyList<BudgetOverrunRow>> GetOverrunsAsync(DateOnly month, CancellationToken ct = default)
    {
        var approved = await Set.AsNoTracking()
            .Include(b => b.Branch)
            .Where(b => b.Month == month && b.Status == BudgetStatus.Approved && b.ApprovedAmountSyp != null)
            .ToListAsync(ct);

        var rows = new List<BudgetOverrunRow>();
        foreach (var budget in approved)
        {
            var limit = budget.ApprovedAmountSyp!.Value;
            var spent = await GetSpentSypAsync(budget.BranchId, month, ct);
            if (spent > limit)
                rows.Add(new BudgetOverrunRow(budget.BranchId, budget.Branch.Name, limit, spent, spent - limit));
        }

        return rows.OrderByDescending(r => r.OverBySyp).ToList();
    }
}