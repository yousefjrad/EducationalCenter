using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Common.Interfaces.Repositories;

public interface IExpenseRepository : IRepository<Expense>
{
    /// <summary>Newest ExpenseDate first. <paramref name="from"/> and <paramref name="to"/> are inclusive.</summary>
    Task<(IReadOnlyList<Expense> Items, int TotalCount)> SearchAsync(
        string? category, DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Distinct categories already used, alphabetical (for suggestions).</summary>
    Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken ct = default);
}
