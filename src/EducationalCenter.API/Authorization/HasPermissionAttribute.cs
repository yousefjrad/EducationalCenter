using Microsoft.AspNetCore.Authorization;

namespace EducationalCenter.API.Authorization;

/// <summary>
/// Protects an endpoint with one permission, for example [HasPermission(Permissions.Payments.Create)].
/// The policy is created on demand by <see cref="PermissionPolicyProvider"/>.
/// </summary>
public sealed class HasPermissionAttribute(string permission)
    : AuthorizeAttribute($"{PermissionPolicyProvider.Prefix}{permission}");
