using Asp.Versioning;
using EducationalCenter.API.Authorization;
using EducationalCenter.Application.Common.Models;
using EducationalCenter.Application.Features.Trainers;
using EducationalCenter.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace EducationalCenter.API.Controllers;

[ApiVersion("1.0")]
public sealed class TrainersController(ITrainerService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.Trainers.View)]
    public async Task<ActionResult<PagedResult<TrainerDto>>> List([FromQuery] TrainerListQuery query, CancellationToken ct) =>
        Ok(await service.ListAsync(query, ct));

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.Trainers.View)]
    public async Task<ActionResult<TrainerDto>> GetById(int id, CancellationToken ct) =>
        Ok(await service.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.Trainers.Create)]
    public async Task<ActionResult<TrainerDto>> Create(CreateTrainerRequest request, CancellationToken ct)
    {
        var dto = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id, version = "1.0" }, dto);
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.Trainers.Update)]
    public async Task<ActionResult<TrainerDto>> Update(int id, UpdateTrainerRequest request, CancellationToken ct) =>
        Ok(await service.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.Trainers.Delete)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}
