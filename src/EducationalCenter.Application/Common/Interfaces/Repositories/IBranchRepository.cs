using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Common.Interfaces.Repositories;

public interface IBranchRepository : IRepository<Branch>
{
    Task<bool> CodeExistsAsync(string code, int? excludeId, CancellationToken ct = default);
    Task<int> CountActiveAsync(CancellationToken ct = default);
}