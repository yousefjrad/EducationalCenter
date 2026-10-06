using Asp.Versioning;
using EducationalCenter.API.Authorization;
using EducationalCenter.Application.Common.Models;
using EducationalCenter.Application.Features.ClassSessions;
using EducationalCenter.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace EducationalCenter.API.Controllers;

[ApiVersion("1.0")]
public sealed class ClassSessionsController(IClassSessionService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.ClassSessions.View)]
    public async Task<ActionResult<PagedResult<ClassSessionDto>>> List([FromQuery] ClassSessionListQuery query, CancellationToken ct) =>
        Ok(await service.ListAsync(query, ct));

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.ClassSessions.View)]
    public async Task<ActionResult<ClassSessionDto>> GetById(int id, CancellationToken ct) =>
        Ok(await service.GetByIdAsync(id, ct));

    /// <summary>Generates all sessions from the section's weekly schedule (once per section).</summary>
    [HttpPost("generate/{sectionId:int}")]
    [HasPermission(Permissions.ClassSessions.Create)]
    public async Task<ActionResult<IReadOnlyList<ClassSessionDto>>> Generate(int sectionId, CancellationToken ct) =>
        Ok(await service.GenerateForSectionAsync(sectionId, ct));

    /// <summary>Adds one extra session manually.</summary>
    [HttpPost]
    [HasPermission(Permissions.ClassSessions.Create)]
    public async Task<ActionResult<ClassSessionDto>> Create(CreateClassSessionRequest request, CancellationToken ct)
    {
        var dto = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id, version = "1.0" }, dto);
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.ClassSessions.Update)]
    public async Task<ActionResult<ClassSessionDto>> Update(int id, UpdateClassSessionRequest request, CancellationToken ct) =>
        Ok(await service.UpdateAsync(id, request, ct));

    /// <summary>Marks the session Postponed and returns the replacement session.</summary>
    [HttpPost("{id:int}/postpone")]
    [HasPermission(Permissions.ClassSessions.Update)]
    public async Task<ActionResult<ClassSessionDto>> Postpone(int id, PostponeClassSessionRequest request, CancellationToken ct) =>
        Ok(await service.PostponeAsync(id, request, ct));

    [HttpPost("{id:int}/cancel")]
    [HasPermission(Permissions.ClassSessions.Update)]
    public async Task<ActionResult<ClassSessionDto>> Cancel(int id, CancelClassSessionRequest request, CancellationToken ct) =>
        Ok(await service.CancelAsync(id, request, ct));
}
