using Asp.Versioning;
using EducationalCenter.API.Authorization;
using EducationalCenter.Application.Features.Grades;
using EducationalCenter.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace EducationalCenter.API.Controllers;

[ApiVersion("1.0")]
public sealed class GradesController(IGradeService service) : ApiControllerBase
{
    /// <summary>Creates or replaces the final grade of an enrollment. Blocked once a certificate is issued.</summary>
    [HttpPut]
    [HasPermission(Permissions.Grades.Record)]
    public async Task<ActionResult<GradeDto>> Record(RecordGradeRequest request, CancellationToken ct) =>
        Ok(await service.RecordAsync(request, ct));

    [HttpGet("enrollments/{enrollmentId:int}")]
    [HasPermission(Permissions.Grades.View)]
    public async Task<ActionResult<GradeDto>> GetByEnrollment(int enrollmentId, CancellationToken ct) =>
        Ok(await service.GetByEnrollmentAsync(enrollmentId, ct));

    [HttpGet("sections/{sectionId:int}")]
    [HasPermission(Permissions.Grades.View)]
    public async Task<ActionResult<IReadOnlyList<GradeDto>>> ListBySection(int sectionId, CancellationToken ct) =>
        Ok(await service.ListBySectionAsync(sectionId, ct));
}
