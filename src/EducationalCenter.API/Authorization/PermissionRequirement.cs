using Microsoft.AspNetCore.Authorization;

namespace EducationalCenter.API.Authorization;

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
