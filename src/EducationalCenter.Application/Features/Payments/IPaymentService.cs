using EducationalCenter.Application.Common.Models;

namespace EducationalCenter.Application.Features.Payments;

public interface IPaymentService
{
    Task<PaymentDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PagedResult<PaymentDto>> ListAsync(PaymentListQuery query, CancellationToken ct = default);

    /// <summary>
    /// Records a cash payment against one installment and issues its receipt, in one transaction.
    /// Overpaying the installment is rejected.
    /// </summary>
    Task<PaymentDto> RecordAsync(RecordPaymentRequest request, CancellationToken ct = default);

    /// <summary>Same as <see cref="RecordAsync"/> for a given user: online payments confirmed by a gateway or by the student.</summary>
    Task<PaymentDto> RecordForUserAsync(RecordPaymentRequest request, int userId, CancellationToken ct = default);

    /// <summary>Admin only (API policy). The payment is kept and marked Cancelled; it is never deleted.</summary>
    Task<PaymentDto> CancelAsync(int id, CancelPaymentRequest request, CancellationToken ct = default);
}
