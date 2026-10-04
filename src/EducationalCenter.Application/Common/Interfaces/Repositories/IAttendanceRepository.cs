using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Common.Interfaces.Repositories;

public interface IAttendanceRepository : IRepository<Attendance>
{
    /// <summary>Tracked. All attendance records of the session.</summary>
    Task<IReadOnlyList<Attendance>> GetBySessionAsync(int sessionId, CancellationToken ct = default);

    /// <summary>All records of the enrollment with ClassSession loaded, ordered by session date then start time.</summary>
    Task<IReadOnlyList<Attendance>> GetByEnrollmentAsync(int enrollmentId, CancellationToken ct = default);
}
