namespace EducationalCenter.Application.Features.Roles;

public sealed record RoleDto(
    int Id,
    string Name,
    string? Description,
    bool IsSystem,
    IReadOnlyList<string> Permissions);

public sealed record PermissionGroupDto(string Module, IReadOnlyList<string> Permissions);

/// <param name="PermissionNames">Names such as "Payments.Create". At least one is required.</param>
public sealed record CreateRoleRequest(string Name, string? Description, IReadOnlyList<string> PermissionNames);

/// <remarks>System roles keep their name. The Admin role cannot be modified at all.</remarks>
public sealed record UpdateRoleRequest(string Name, string? Description, IReadOnlyList<string> PermissionNames);
