using Asp.Versioning;
using EducationalCenter.API.Authorization;
using EducationalCenter.Application.Common.Models;
using EducationalCenter.Application.Features.Students;
using EducationalCenter.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace EducationalCenter.API.Controllers;

[ApiVersion("1.0")]
public sealed class StudentsController(IStudentService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.Students.View)]
    public async Task<ActionResult<PagedResult<StudentDto>>> List([FromQuery] StudentListQuery query, CancellationToken ct) =>
        Ok(await service.ListAsync(query, ct));

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.Students.View)]
    public async Task<ActionResult<StudentDto>> GetById(int id, CancellationToken ct) =>
        Ok(await service.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.Students.Create)]
    public async Task<ActionResult<StudentDto>> Create(CreateStudentRequest request, CancellationToken ct)
    {
        var dto = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id, version = "1.0" }, dto);
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.Students.Update)]
    public async Task<ActionResult<StudentDto>> Update(int id, UpdateStudentRequest request, CancellationToken ct) =>
        Ok(await service.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.Students.Delete)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}
