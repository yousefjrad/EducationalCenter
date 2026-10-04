using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Common.Interfaces.Repositories;

public interface IRoleRepository : IRepository<Role>
{
    /// <summary>Tracked. Loads RolePermissions with their Permission.</summary>
    Task<Role?> GetWithPermissionsAsync(int id, CancellationToken ct = default);

    /// <summary>Roles with RolePermissions and Permission loaded, ordered by name.</summary>
    Task<IReadOnlyList<Role>> ListWithPermissionsAsync(CancellationToken ct = default);

    Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken ct = default);

    Task<bool> HasUsersAsync(int roleId, CancellationToken ct = default);

    /// <summary>RolePermission has a composite key and no soft delete: these rows are really deleted.</summary>
    void RemoveRolePermissions(IEnumerable<RolePermission> items);
}
