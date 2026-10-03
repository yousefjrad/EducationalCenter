using EducationalCenter.Domain.Common;
using EducationalCenter.Domain.Enums;

namespace EducationalCenter.Domain.Entities;

public class Installment : BaseEntity
{
    public int PaymentPlanId { get; set; }
    public PaymentPlan PaymentPlan { get; set; } = null!;

    public int Number { get; set; }
    public decimal Amount { get; set; }
    public DateOnly DueDate { get; set; }
    /// <summary>Overdue is never stored; it is computed when querying.</summary>
    public InstallmentStatus Status { get; set; } = InstallmentStatus.Unpaid;

    public ICollection<Payment> Payments { get; set; } = [];
}
