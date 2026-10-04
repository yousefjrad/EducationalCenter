using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Common.Interfaces.Repositories;

public interface IUserRepository : IRepository<User>
{
    /// <summary>Tracked. Loads Role.</summary>
    Task<User?> GetWithRoleAsync(int id, CancellationToken ct = default);

    /// <summary>Tracked. Loads Role. Case-insensitive match on email.</summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);

    Task<bool> EmailExistsAsync(string email, int? excludeId, CancellationToken ct = default);

    Task<int> CountActiveInRoleAsync(int roleId, CancellationToken ct = default);

    /// <summary>Permission names granted through the user's role (read on every request, so cache carefully).</summary>
    Task<IReadOnlyList<string>> GetPermissionNamesAsync(int userId, CancellationToken ct = default);

    /// <summary>Items come with Role loaded, ordered by name. Search matches name or email.</summary>
    Task<(IReadOnlyList<User> Items, int TotalCount)> SearchAsync(
        string? search, int? roleId, bool? isActive, int page, int pageSize, CancellationToken ct = default);
}
