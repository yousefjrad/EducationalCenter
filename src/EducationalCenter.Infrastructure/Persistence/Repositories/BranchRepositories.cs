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
}