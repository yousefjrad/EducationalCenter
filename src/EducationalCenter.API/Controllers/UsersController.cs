using Asp.Versioning;
using EducationalCenter.API.Authorization;
using EducationalCenter.Application.Common.Models;
using EducationalCenter.Application.Features.Users;
using EducationalCenter.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace EducationalCenter.API.Controllers;

[ApiVersion("1.0")]
public sealed class UsersController(IUserService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.Users.View)]
    public async Task<ActionResult<PagedResult<UserDto>>> List([FromQuery] UserListQuery query, CancellationToken ct) =>
        Ok(await service.ListAsync(query, ct));

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.Users.View)]
    public async Task<ActionResult<UserDto>> GetById(int id, CancellationToken ct) =>
        Ok(await service.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.Users.Create)]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request, CancellationToken ct)
    {
        var dto = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id, version = "1.0" }, dto);
    }

    /// <summary>Users are never deleted: set isActive to false to deactivate. Deactivating or changing the role ends their sessions.</summary>
    [HttpPut("{id:int}")]
    [HasPermission(Permissions.Users.Update)]
    public async Task<ActionResult<UserDto>> Update(int id, UpdateUserRequest request, CancellationToken ct) =>
        Ok(await service.UpdateAsync(id, request, ct));

    /// <summary>Clears a failed-sign-in lock now instead of waiting for it to expire.</summary>
    [HttpPost("{id:int}/unlock")]
    [HasPermission(Permissions.Users.Update)]
    public async Task<ActionResult<UserDto>> Unlock(int id, CancellationToken ct) =>
        Ok(await service.UnlockAsync(id, ct));

    /// <summary>Sets a new password for another user and ends their sessions.</summary>
    [HttpPost("{id:int}/reset-password")]
    [HasPermission(Permissions.Users.Update)]
    public async Task<IActionResult> ResetPassword(int id, ResetPasswordRequest request, CancellationToken ct)
    {
        await service.ResetPasswordAsync(id, request, ct);
        return NoContent();
    }
}
