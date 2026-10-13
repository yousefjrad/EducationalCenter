using EducationalCenter.Application.Common.Interfaces.Repositories;
using EducationalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EducationalCenter.Infrastructure.Persistence.Repositories;

internal sealed class BranchRepository(AppDbContext context) : Repository<Branch>(context), IBranchRepository
{
    public Task<bool> CodeExistsAsync(string code, int? excludeId, CancellationToken ct = default) =>
        Set.AnyAsync(b => b.Code == code && (excludeId == null || b.Id != excludeId), ct);

    public Task<int> CountActiveAsync(CancellationToken ct = default) =>
        Set.CountAsync(b => b.IsActive, ct);

    public Task<(IReadOnlyList<Branch> Items, int TotalCount)> SearchAsync(
        string? search, bool? isActive, int? onlyBranchId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = Set.AsNoTracking().AsQueryable();

        if (onlyBranchId.HasValue)
            query = query.Where(b => b.Id == onlyBranchId.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(b => b.Name.Contains(search) || b.Code.Contains(search) || (b.Governorate != null && b.Governorate.Contains(search)));
        if (isActive.HasValue)
            query = query.Where(b => b.IsActive == isActive.Value);

        return query.OrderBy(b => b.Name).ThenBy(b => b.Id).ToPageAsync(page, pageSize, ct);
    }
}