using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Enums;

namespace EducationalCenter.Application.Features.PaymentPlans;

/// <summary>Balances are always computed from valid payments, never stored.</summary>
internal static class PaymentMath
{
    public static decimal PaidSyp(Installment installment) =>
        installment.Payments.Where(p => p.Status == PaymentStatus.Valid).Sum(p => p.AmountInSyp);

    /// <summary>The persisted status: Unpaid, PartiallyPaid or Paid.</summary>
    public static InstallmentStatus StoredStatus(decimal paidSyp, decimal amount) =>
        paidSyp <= 0m ? InstallmentStatus.Unpaid
        : paidSyp >= amount ? InstallmentStatus.Paid
        : InstallmentStatus.PartiallyPaid;

    /// <summary>The status shown to users: adds Overdue for open plans.</summary>
    public static InstallmentStatus DisplayStatus(Installment installment, PaymentPlanStatus planStatus, DateOnly today) =>
        planStatus == PaymentPlanStatus.Open
        && installment.Status != InstallmentStatus.Paid
        && installment.DueDate < today
            ? InstallmentStatus.Overdue
            : installment.Status;
}
