using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Enums;

namespace EducationalCenter.Application.Common.Interfaces.Repositories;

public interface IPaymentRepository : IRepository<Payment>
{
    /// <summary>
    /// Tracked. Loads Receipt and Installment with PaymentPlan, Enrollment, Student and Section.
    /// </summary>
    Task<Payment?> GetWithDetailsAsync(int id, CancellationToken ct = default);

    /// <summary>All payments (any status) of an enrollment's plan. No navigation properties needed.</summary>
    Task<IReadOnlyList<Payment>> GetByEnrollmentAsync(int enrollmentId, CancellationToken ct = default);

    /// <summary>
    /// Items come with the same details as <see cref="GetWithDetailsAsync"/>, newest first.
    /// <paramref name="from"/> and <paramref name="to"/> filter on the UTC date of PaidAt (inclusive).
    /// </summary>
    Task<(IReadOnlyList<Payment> Items, int TotalCount)> SearchAsync(
        int? enrollmentId, int? studentId, DateOnly? from, DateOnly? to, PaymentStatus? status,
        int page, int pageSize, CancellationToken ct = default);

    /// <summary>
    /// Sum of AmountInSyp of Valid payments received between the two UTC dates (inclusive) for enrollments
    /// in sections taught by the trainer (Section.TrainerId). Zero if there are none.
    /// </summary>
    Task<decimal> SumValidInSypByTrainerAsync(int trainerId, DateOnly from, DateOnly to, CancellationToken ct = default);
}
