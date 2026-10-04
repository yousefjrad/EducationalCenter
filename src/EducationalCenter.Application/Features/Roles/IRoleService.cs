namespace EducationalCenter.Application.Features.Roles;

public interface IRoleService
{
    Task<RoleDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<RoleDto>> ListAsync(CancellationToken ct = default);

    /// <summary>Every permission that exists, grouped by module, for the role editor.</summary>
    Task<IReadOnlyList<PermissionGroupDto>> ListPermissionsAsync(CancellationToken ct = default);

    Task<RoleDto> CreateAsync(CreateRoleRequest request, CancellationToken ct = default);
    Task<RoleDto> UpdateAsync(int id, UpdateRoleRequest request, CancellationToken ct = default);

    /// <summary>System roles and roles that still have users cannot be deleted.</summary>
    Task DeleteAsync(int id, CancellationToken ct = default);
}
