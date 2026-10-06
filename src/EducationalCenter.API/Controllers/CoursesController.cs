using Asp.Versioning;
using EducationalCenter.API.Authorization;
using EducationalCenter.Application.Common.Models;
using EducationalCenter.Application.Features.Courses;
using EducationalCenter.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace EducationalCenter.API.Controllers;

[ApiVersion("1.0")]
public sealed class CoursesController(ICourseService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.Courses.View)]
    public async Task<ActionResult<PagedResult<CourseDto>>> List([FromQuery] CourseListQuery query, CancellationToken ct) =>
        Ok(await service.ListAsync(query, ct));

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.Courses.View)]
    public async Task<ActionResult<CourseDto>> GetById(int id, CancellationToken ct) =>
        Ok(await service.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.Courses.Create)]
    public async Task<ActionResult<CourseDto>> Create(CreateCourseRequest request, CancellationToken ct)
    {
        var dto = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id, version = "1.0" }, dto);
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.Courses.Update)]
    public async Task<ActionResult<CourseDto>> Update(int id, UpdateCourseRequest request, CancellationToken ct) =>
        Ok(await service.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.Courses.Delete)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}
