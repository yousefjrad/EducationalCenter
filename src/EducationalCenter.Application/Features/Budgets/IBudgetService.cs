using EducationalCenter.Application.Common.Models;

namespace EducationalCenter.Application.Features.Budgets;

public interface IBudgetService
{
    Task<BudgetDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PagedResult<BudgetDto>> ListAsync(BudgetListQuery query, CancellationToken ct = default);

    /// <summary>A draft for the current branch. A branch has one budget per month.</summary>
    Task<BudgetDto> CreateAsync(CreateBudgetRequest request, CancellationToken ct = default);

    /// <summary>Only a draft or a rejected budget can be edited (a rejected one becomes a draft again).</summary>
    Task<BudgetDto> UpdateAsync(int id, UpdateBudgetRequest request, CancellationToken ct = default);

    Task<BudgetDto> SubmitAsync(int id, CancellationToken ct = default);
    Task<BudgetDto> ApproveAsync(int id, ApproveBudgetRequest request, CancellationToken ct = default);
    Task<BudgetDto> RejectAsync(int id, RejectBudgetRequest request, CancellationToken ct = default);
}