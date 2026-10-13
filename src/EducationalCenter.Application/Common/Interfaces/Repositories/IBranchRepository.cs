using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Common.Interfaces.Repositories;

public interface IBranchRepository : IRepository<Branch>
{
    Task<bool> CodeExistsAsync(string code, int? excludeId, CancellationToken ct = default);

    Task<int> CountActiveAsync(CancellationToken ct = default);

    /// <summary>onlyBranchId limits the list to one branch (null = all).</summary>
    Task<(IReadOnlyList<Branch> Items, int TotalCount)> SearchAsync(
        string? search, bool? isActive, int? onlyBranchId, int page, int pageSize, CancellationToken ct = default);
}