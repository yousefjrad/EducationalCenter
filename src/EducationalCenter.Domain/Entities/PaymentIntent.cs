using EducationalCenter.Domain.Common;
using EducationalCenter.Domain.Enums;

namespace EducationalCenter.Domain.Entities;

/// <summary>
/// A student's request to pay (part of) an installment online. It becomes a real Payment with a receipt
/// only when the payment is confirmed. Never deleted.
/// </summary>
public class PaymentIntent : BaseEntity
{
    public int InstallmentId { get; set; }
    public Installment Installment { get; set; } = null!;

    /// <summary>The student's sign-in account that started the payment.</summary>
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Our own unique reference, sent to the provider.</summary>
    public string Reference { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string? ProviderReference { get; set; }
    public string? RedirectUrl { get; set; }

    public decimal AmountInSyp { get; set; }
    public PaymentIntentStatus Status { get; set; } = PaymentIntentStatus.Pending;
    public DateTime ExpiresAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? PaymentId { get; set; }
    public string? FailureReason { get; set; }
}