using EducationalCenter.Application.Features.Budgets;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Enums;

namespace EducationalCenter.Application.Common.Interfaces.Repositories;

public interface IBudgetRepository : IRepository<Budget>
{
    /// <summary>Tracked. Loads Branch and RequestedByUser.</summary>
    Task<Budget?> GetWithDetailsAsync(int id, CancellationToken ct = default);

    Task<bool> ExistsAsync(DateOnly month, int branchId, int? excludeId, CancellationToken ct = default);

    /// <summary>Newest month first. Items come with Branch and RequestedByUser.</summary>
    Task<(IReadOnlyList<Budget> Items, int TotalCount)> SearchAsync(
        BudgetStatus? status, DateOnly? from, DateOnly? to, int? branchId, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Expenses of the branch in that month (SYP), within the current branch scope.</summary>
    Task<decimal> GetSpentSypAsync(int branchId, DateOnly month, CancellationToken ct = default);

    /// <summary>Approved budgets of that month whose spending passed the approved amount, within the current branch scope.</summary>
    Task<IReadOnlyList<BudgetOverrunRow>> GetOverrunsAsync(DateOnly month, CancellationToken ct = default);
}