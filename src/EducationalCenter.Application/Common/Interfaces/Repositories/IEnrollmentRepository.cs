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

    /// <summary>Tracked. Loads Student, Section (with Course), PaymentPlan, Certificate and Grade.</summary>
    Task<Enrollment?> GetWithDetailsAsync(int id, CancellationToken ct = default);

    /// <summary>Tracked. Pending enrollments whose HoldExpiresAt is on or before <paramref name="utcNow"/>.</summary>
    Task<IReadOnlyList<Enrollment>> GetExpiredHoldsAsync(DateTime utcNow, CancellationToken ct = default);

    /// <summary>Items come with Student and Section (with Course) loaded, newest first.</summary>
    Task<(IReadOnlyList<Enrollment> Items, int TotalCount)> SearchAsync(
        int? studentId, int? sectionId, EnrollmentStatus? status,
        int page, int pageSize, CancellationToken ct = default);

    /// <summary>Tracked, with Student loaded. Enrollments of the section that are Confirmed or Completed.</summary>
    Task<IReadOnlyList<Enrollment>> GetEnrolledBySectionAsync(int sectionId, CancellationToken ct = default);

    /// <summary>
    /// Sum of AmountInSyp of the enrollment's payments with status Valid
    /// (across all installments of its payment plan). Zero if there is no plan or no payment.
    /// </summary>
    Task<decimal> GetPaidAmountInSypAsync(int enrollmentId, CancellationToken ct = default);

    /// <summary>
    /// Tracked. The student's Confirmed or Completed enrollment in the section, or null, with
    /// PaymentPlan, its Installments and their Payments (all statuses) loaded. Used by the legacy payments import.
    /// </summary>
    Task<Enrollment?> GetForLegacyImportAsync(int studentId, int sectionId, CancellationToken ct = default);

    /// <summary>
    /// Every Pending enrollment (seat hold waiting for confirmation), earliest HoldExpiresAt first,
    /// with Student and Section loaded. Includes holds that already expired but were not cancelled yet.
    /// </summary>
    Task<IReadOnlyList<Enrollment>> GetPendingAsync(CancellationToken ct = default);
}
