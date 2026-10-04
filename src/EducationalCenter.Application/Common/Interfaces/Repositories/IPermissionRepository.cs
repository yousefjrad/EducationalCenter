using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Common.Interfaces.Repositories;

/// <summary>Permissions are seeded from code and never created by users, so this is read-only.</summary>
public interface IPermissionRepository
{
    /// <summary>Tracked. Only the permissions that exist in the database.</summary>
    Task<IReadOnlyList<Permission>> GetByNamesAsync(IReadOnlyCollection<string> names, CancellationToken ct = default);
}
