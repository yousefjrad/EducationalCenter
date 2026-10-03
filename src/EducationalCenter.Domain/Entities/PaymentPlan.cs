using EducationalCenter.Domain.Common;
using EducationalCenter.Domain.Enums;

namespace EducationalCenter.Domain.Entities;

public class PaymentPlan : BaseEntity
{
    public int EnrollmentId { get; set; }
    public Enrollment Enrollment { get; set; } = null!;

    /// <summary>Always in SYP.</summary>
    public decimal TotalAmount { get; set; }
    public int InstallmentsCount { get; set; }
    public PaymentPlanStatus Status { get; set; } = PaymentPlanStatus.Open;

    public ICollection<Installment> Installments { get; set; } = [];
}
