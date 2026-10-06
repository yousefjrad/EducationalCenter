using Asp.Versioning;
using EducationalCenter.API.Authorization;
using EducationalCenter.Application.Features.Attendances;
using EducationalCenter.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace EducationalCenter.API.Controllers;

[ApiVersion("1.0")]
public sealed class AttendanceController(IAttendanceService service) : ApiControllerBase
{
    /// <summary>The session roster (confirmed students) with the attendance recorded so far.</summary>
    [HttpGet("sessions/{sessionId:int}")]
    [HasPermission(Permissions.Attendance.View)]
    public async Task<ActionResult<SessionAttendanceDto>> GetSession(int sessionId, CancellationToken ct) =>
        Ok(await service.GetSessionAttendanceAsync(sessionId, ct));

    /// <summary>Creates or updates the marks; the first record marks the session as Held.</summary>
    [HttpPut("sessions/{sessionId:int}")]
    [HasPermission(Permissions.Attendance.Record)]
    public async Task<ActionResult<SessionAttendanceDto>> Record(int sessionId, RecordAttendanceRequest request, CancellationToken ct) =>
        Ok(await service.RecordAsync(sessionId, request, ct));

    [HttpGet("enrollments/{enrollmentId:int}")]
    [HasPermission(Permissions.Attendance.View)]
    public async Task<ActionResult<EnrollmentAttendanceDto>> GetEnrollmentSummary(int enrollmentId, CancellationToken ct) =>
        Ok(await service.GetEnrollmentSummaryAsync(enrollmentId, ct));
}
