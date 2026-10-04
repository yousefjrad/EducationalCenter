using EducationalCenter.Application.Common.Models;

namespace EducationalCenter.Application.Features.Expenses;

public interface IExpenseService
{
    Task<ExpenseDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PagedResult<ExpenseDto>> ListAsync(ExpenseListQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken ct = default);

    Task<ExpenseDto> CreateAsync(CreateExpenseRequest request, CancellationToken ct = default);

    /// <summary>Expenses are financial records: they can be corrected (audited) but never deleted.</summary>
    Task<ExpenseDto> UpdateAsync(int id, UpdateExpenseRequest request, CancellationToken ct = default);
}
