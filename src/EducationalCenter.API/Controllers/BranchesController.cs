using Asp.Versioning;
using EducationalCenter.API.Authorization;
using EducationalCenter.Application.Features.Branches;
using EducationalCenter.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace EducationalCenter.API.Controllers;

[ApiVersion("1.0")]
public sealed class BranchesController(IBranchService service, IBranchLinkService links) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.Branches.View)]
    public async Task<ActionResult<IReadOnlyList<BranchDto>>> List(CancellationToken ct) =>
        Ok(await service.ListAsync(ct));

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.Branches.View)]
    public async Task<ActionResult<BranchDto>> GetById(int id, CancellationToken ct) =>
        Ok(await service.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.Branches.Manage)]
    public async Task<ActionResult<BranchDto>> Create(CreateBranchRequest request, CancellationToken ct)
    {
        var dto = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id, version = "1.0" }, dto);
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.Branches.Manage)]
    public async Task<ActionResult<BranchDto>> Update(int id, UpdateBranchRequest request, CancellationToken ct) =>
        Ok(await service.UpdateAsync(id, request, ct));

    [HttpPost("{id:int}/close")]
    [HasPermission(Permissions.Branches.Manage)]
    public async Task<ActionResult<BranchDto>> Close(int id, CancellationToken ct) =>
        Ok(await service.CloseAsync(id, ct));

    [HttpPost("{id:int}/reopen")]
    [HasPermission(Permissions.Branches.Manage)]
    public async Task<ActionResult<BranchDto>> Reopen(int id, CancellationToken ct) =>
        Ok(await service.ReopenAsync(id, ct));

    // ---- Courses the branch offers (enabled from the global catalog) ----

    [HttpGet("{id:int}/courses")]
    [HasPermission(Permissions.Branches.View)]
    public async Task<ActionResult<IReadOnlyList<BranchCourseDto>>> ListCourses(int id, CancellationToken ct) =>
        Ok(await links.ListCoursesAsync(id, ct));

    [HttpPut("{id:int}/courses/{courseId:int}")]
    [HasPermission(Permissions.Branches.Manage)]
    public async Task<ActionResult<BranchCourseDto>> EnableCourse(int id, int courseId, CancellationToken ct) =>
        Ok(await links.EnableCourseAsync(id, courseId, ct));

    [HttpDelete("{id:int}/courses/{courseId:int}")]
    [HasPermission(Permissions.Branches.Manage)]
    public async Task<ActionResult<BranchCourseDto>> DisableCourse(int id, int courseId, CancellationToken ct) =>
        Ok(await links.DisableCourseAsync(id, courseId, ct));

    // ---- Trainers who teach at the branch ----

    [HttpGet("{id:int}/trainers")]
    [HasPermission(Permissions.Branches.View)]
    public async Task<ActionResult<IReadOnlyList<BranchTrainerDto>>> ListTrainers(int id, CancellationToken ct) =>
        Ok(await links.ListTrainersAsync(id, ct));

    [HttpPut("{id:int}/trainers/{trainerId:int}")]
    [HasPermission(Permissions.Branches.Manage)]
    public async Task<ActionResult<BranchTrainerDto>> AddTrainer(int id, int trainerId, CancellationToken ct) =>
        Ok(await links.AddTrainerAsync(id, trainerId, ct));

    [HttpDelete("{id:int}/trainers/{trainerId:int}")]
    [HasPermission(Permissions.Branches.Manage)]
    public async Task<IActionResult> RemoveTrainer(int id, int trainerId, CancellationToken ct)
    {
        await links.RemoveTrainerAsync(id, trainerId, ct);
        return NoContent();
    }
}