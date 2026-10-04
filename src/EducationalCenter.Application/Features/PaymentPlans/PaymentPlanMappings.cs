using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Features.PaymentPlans;

public static class PaymentPlanMappings
{
    /// <remarks>Installments (with Payments) and Enrollment (with Student and Section) must be loaded.</remarks>
    public static PaymentPlanDto ToDto(this PaymentPlan plan, DateOnly today)
    {
        var installments = plan.Installments
            .OrderBy(i => i.Number)
            .Select(i =>
            {
                var paid = PaymentMath.PaidSyp(i);
                return new InstallmentDto(
                    i.Id,
                    i.Number,
                    i.Amount,
                    i.DueDate,
                    paid,
                    Math.Max(0m, i.Amount - paid),
                    PaymentMath.DisplayStatus(i, plan.Status, today));
            })
            .ToList();

        var totalPaid = installments.Sum(i => i.PaidAmountInSyp);

        return new PaymentPlanDto(
            plan.Id,
            plan.EnrollmentId,
            plan.Enrollment.StudentId,
            plan.Enrollment.Student.FullName,
            plan.Enrollment.Section.Name,
            plan.TotalAmount,
            plan.InstallmentsCount,
            totalPaid,
            plan.TotalAmount - totalPaid,
            plan.Status,
            installments);
    }
}
