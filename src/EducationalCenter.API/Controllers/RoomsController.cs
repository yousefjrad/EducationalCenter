using Asp.Versioning;
using EducationalCenter.API.Authorization;
using EducationalCenter.Application.Common.Models;
using EducationalCenter.Application.Features.Rooms;
using EducationalCenter.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace EducationalCenter.API.Controllers;

[ApiVersion("1.0")]
public sealed class RoomsController(IRoomService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.Rooms.View)]
    public async Task<ActionResult<PagedResult<RoomDto>>> List([FromQuery] RoomListQuery query, CancellationToken ct) =>
        Ok(await service.ListAsync(query, ct));

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.Rooms.View)]
    public async Task<ActionResult<RoomDto>> GetById(int id, CancellationToken ct) =>
        Ok(await service.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.Rooms.Create)]
    public async Task<ActionResult<RoomDto>> Create(CreateRoomRequest request, CancellationToken ct)
    {
        var dto = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id, version = "1.0" }, dto);
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.Rooms.Update)]
    public async Task<ActionResult<RoomDto>> Update(int id, UpdateRoomRequest request, CancellationToken ct) =>
        Ok(await service.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.Rooms.Delete)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}
