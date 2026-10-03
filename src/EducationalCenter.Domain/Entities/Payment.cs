using EducationalCenter.Domain.Common;
using EducationalCenter.Domain.Enums;

namespace EducationalCenter.Domain.Entities;

/// <summary>Financial record: never hard-deleted; cancelled by changing Status.</summary>
public class Payment : BaseEntity
{
    public int InstallmentId { get; set; }
    public Installment Installment { get; set; } = null!;

    public Currency Currency { get; set; } = Currency.Syp;
    public decimal AmountPaid { get; set; }
    /// <summary>SYP per 1 USD, entered by staff; null when paid in SYP.</summary>
    public decimal? ExchangeRate { get; set; }
    public decimal AmountInSyp { get; set; }
    public DateTime PaidAt { get; set; }
    public int ReceivedByUserId { get; set; }
    public User ReceivedByUser { get; set; } = null!;
    public PaymentStatus Status { get; set; } = PaymentStatus.Valid;
    public string? CancelReason { get; set; }

    public Receipt? Receipt { get; set; }
}
