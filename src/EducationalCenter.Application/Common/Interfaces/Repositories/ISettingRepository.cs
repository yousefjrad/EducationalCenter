using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Common.Interfaces.Repositories;

public interface ISettingRepository
{
    /// <summary>Tracked.</summary>
    Task<Setting?> GetByKeyAsync(string key, CancellationToken ct = default);

    /// <summary>Ordered by key.</summary>
    Task<IReadOnlyList<Setting>> ListAsync(CancellationToken ct = default);
}
