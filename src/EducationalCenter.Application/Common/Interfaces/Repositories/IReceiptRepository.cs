using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Common.Interfaces.Repositories;

public interface IReceiptRepository : IRepository<Receipt>
{
    /// <summary>
    /// Loads Payment (with ReceivedByUser) and its Installment, PaymentPlan, Enrollment,
    /// Student and Section (with Course).
    /// </summary>
    Task<Receipt?> GetWithDetailsAsync(int id, CancellationToken ct = default);

    /// <summary>Same details as <see cref="GetWithDetailsAsync"/>.</summary>
    Task<Receipt?> GetByPaymentIdAsync(int paymentId, CancellationToken ct = default);
}
