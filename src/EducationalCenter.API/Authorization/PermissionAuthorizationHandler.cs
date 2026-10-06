using Microsoft.AspNetCore.Authorization;

namespace EducationalCenter.API.Authorization;

public sealed class PermissionAuthorizationHandler(UserPermissionProvider permissions)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (!int.TryParse(context.User.FindFirst("sub")?.Value, out var userId))
            return;

        if (await permissions.HasAsync(userId, requirement.Permission, CancellationToken.None))
            context.Succeed(requirement);
    }
}
