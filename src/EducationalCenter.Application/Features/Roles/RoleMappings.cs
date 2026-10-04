using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Features.Roles;

public static class RoleMappings
{
    /// <remarks>RolePermissions with their Permission must be loaded.</remarks>
    public static RoleDto ToDto(this Role r) => new(
        r.Id,
        r.Name,
        r.Description,
        r.IsSystem,
        r.RolePermissions.Select(rp => rp.Permission.Name).OrderBy(n => n).ToList());
}
