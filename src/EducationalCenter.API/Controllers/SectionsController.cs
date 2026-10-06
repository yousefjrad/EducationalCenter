using Asp.Versioning;
using EducationalCenter.API.Authorization;
using EducationalCenter.Application.Common.Models;
using EducationalCenter.Application.Features.Sections;
using EducationalCenter.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace EducationalCenter.API.Controllers;

[ApiVersion("1.0")]
public sealed class SectionsController(ISectionService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.Sections.View)]
    public async Task<ActionResult<PagedResult<SectionDto>>> List([FromQuery] SectionListQuery query, CancellationToken ct) =>
        Ok(await service.ListAsync(query, ct));

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.Sections.View)]
    public async Task<ActionResult<SectionDto>> GetById(int id, CancellationToken ct) =>
        Ok(await service.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.Sections.Create)]
    public async Task<ActionResult<SectionDto>> Create(CreateSectionRequest request, CancellationToken ct)
    {
        var dto = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id, version = "1.0" }, dto);
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.Sections.Update)]
    public async Task<ActionResult<SectionDto>> Update(int id, UpdateSectionRequest request, CancellationToken ct) =>
        Ok(await service.UpdateAsync(id, request, ct));

    /// <summary>Moves the section along Draft -> OpenForEnrollment -> InProgress -> Completed, or cancels it.</summary>
    [HttpPost("{id:int}/status")]
    [HasPermission(Permissions.Sections.Update)]
    public async Task<ActionResult<SectionDto>> ChangeStatus(int id, ChangeSectionStatusRequest request, CancellationToken ct) =>
        Ok(await service.ChangeStatusAsync(id, request, ct));

    /// <summary>Only an empty draft section can be deleted.</summary>
    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.Sections.Delete)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}
