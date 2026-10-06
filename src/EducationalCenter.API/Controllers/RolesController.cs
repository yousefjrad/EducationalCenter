using Asp.Versioning;
using EducationalCenter.API.Authorization;
using EducationalCenter.Application.Features.Roles;
using EducationalCenter.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace EducationalCenter.API.Controllers;

[ApiVersion("1.0")]
public sealed class RolesController(IRoleService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.Roles.View)]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> List(CancellationToken ct) =>
        Ok(await service.ListAsync(ct));

    /// <summary>Every permission that exists, grouped by module, for the role editor.</summary>
    [HttpGet("permissions")]
    [HasPermission(Permissions.Roles.View)]
    public async Task<ActionResult<IReadOnlyList<PermissionGroupDto>>> ListPermissions(CancellationToken ct) =>
        Ok(await service.ListPermissionsAsync(ct));

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.Roles.View)]
    public async Task<ActionResult<RoleDto>> GetById(int id, CancellationToken ct) =>
        Ok(await service.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.Roles.Manage)]
    public async Task<ActionResult<RoleDto>> Create(CreateRoleRequest request, CancellationToken ct)
    {
        var dto = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id, version = "1.0" }, dto);
    }

    /// <summary>System roles keep their name. The Admin role cannot be modified at all.</summary>
    [HttpPut("{id:int}")]
    [HasPermission(Permissions.Roles.Manage)]
    public async Task<ActionResult<RoleDto>> Update(int id, UpdateRoleRequest request, CancellationToken ct) =>
        Ok(await service.UpdateAsync(id, request, ct));

    /// <summary>System roles and roles that still have users cannot be deleted.</summary>
    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.Roles.Manage)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}
