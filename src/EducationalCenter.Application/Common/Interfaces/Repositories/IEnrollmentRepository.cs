using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Enums;

namespace EducationalCenter.Application.Common.Interfaces.Repositories;

public interface IEnrollmentRepository : IRepository<Enrollment>
{
    /// <summary>Seats taken: Confirmed enrollments plus Pending ones whose hold has not expired.</summary>
    Task<int> CountSeatsTakenAsync(int sectionId, DateTime utcNow, CancellationToken ct = default);

    /// <summary>
    /// True if the student already has an enrollment in the section with status
    /// Confirmed, Completed, or Pending with an unexpired hold.
    /// </summary>
    Task<bool> HasActiveEnrollmentAsync(int studentId, int sectionId, DateTime utcNow, CancellationToken ct = default);

    /// <summary>Tracked. Loads Student, Section (with Course), PaymentPlan and Certificate.</summary>
    Task<Enrollment?> GetWithDetailsAsync(int id, CancellationToken ct = default);

    /// <summary>Tracked. Pending enrollments whose HoldExpiresAt is on or before <paramref name="utcNow"/>.</summary>
    Task<IReadOnlyList<Enrollment>> GetExpiredHoldsAsync(DateTime utcNow, CancellationToken ct = default);

    /// <summary>Items come with Student and Section (with Course) loaded, newest first.</summary>
    Task<(IReadOnlyList<Enrollment> Items, int TotalCount)> SearchAsync(
        int? studentId, int? sectionId, EnrollmentStatus? status,
        int page, int pageSize, CancellationToken ct = default);
}
