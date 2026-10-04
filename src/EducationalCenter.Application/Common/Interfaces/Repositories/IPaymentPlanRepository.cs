using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Common.Interfaces.Repositories;

public interface IPaymentPlanRepository : IRepository<PaymentPlan>
{
    /// <summary>
    /// Tracked. Loads Installments (with all their Payments) and Enrollment (with Student and Section).
    /// </summary>
    Task<PaymentPlan?> GetByEnrollmentIdAsync(int enrollmentId, CancellationToken ct = default);

    /// <summary>Same graph as <see cref="GetByEnrollmentIdAsync"/>, found through one of its installments.</summary>
    Task<PaymentPlan?> GetByInstallmentIdAsync(int installmentId, CancellationToken ct = default);
}
