namespace EducationalCenter.Application.Features.PaymentPlans;

public interface IPaymentPlanService
{
    /// <summary>
    /// Creates the enrollment's plan from its agreed price (no discounts) split into equal monthly
    /// installments; any remainder goes to the first one. A count of 1 is a full payment.
    /// </summary>
    Task<PaymentPlanDto> CreateAsync(CreatePaymentPlanRequest request, CancellationToken ct = default);

    Task<PaymentPlanDto> GetByEnrollmentAsync(int enrollmentId, CancellationToken ct = default);
}
