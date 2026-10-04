namespace EducationalCenter.Application.Features.Attendances;

public interface IAttendanceService
{
    /// <summary>The roster of the session (confirmed students) with their recorded attendance.</summary>
    Task<SessionAttendanceDto> GetSessionAttendanceAsync(int sessionId, CancellationToken ct = default);

    /// <summary>Creates or updates marks for the given students; the first record marks the session as Held.</summary>
    Task<SessionAttendanceDto> RecordAsync(int sessionId, RecordAttendanceRequest request, CancellationToken ct = default);

    Task<EnrollmentAttendanceDto> GetEnrollmentSummaryAsync(int enrollmentId, CancellationToken ct = default);
}
