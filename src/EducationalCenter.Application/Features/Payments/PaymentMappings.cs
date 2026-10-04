using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Features.Payments;

public static class PaymentMappings
{
    /// <remarks>Installment (with PaymentPlan, Enrollment, Student, Section) and Receipt must be loaded.</remarks>
    public static PaymentDto ToDto(this Payment p)
    {
        var enrollment = p.Installment.PaymentPlan.Enrollment;

        return new PaymentDto(
            p.Id,
            p.InstallmentId,
            p.Installment.Number,
            enrollment.Id,
            enrollment.Student.FullName,
            enrollment.Section.Name,
            p.Currency,
            p.AmountPaid,
            p.ExchangeRate,
            p.AmountInSyp,
            p.PaidAt,
            p.ReceivedByUserId,
            p.Status,
            p.CancelReason,
            p.Receipt?.Id,
            p.Receipt?.ReceiptNumber);
    }
}
