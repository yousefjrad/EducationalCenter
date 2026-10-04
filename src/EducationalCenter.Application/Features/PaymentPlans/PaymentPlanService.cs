using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Enums;
using EducationalCenter.Domain.Exceptions;

namespace EducationalCenter.Application.Features.PaymentPlans;

public sealed class PaymentPlanService(IUnitOfWork uow, IClock clock, IAuditLogger audit) : IPaymentPlanService
{
    public Task<PaymentPlanDto> CreateAsync(CreatePaymentPlanRequest request, CancellationToken ct = default)
    {
        return uow.ExecuteInTransactionAsync(async () =>
        {
            var enrollment = await uow.Enrollments.GetWithDetailsAsync(request.EnrollmentId, ct)
                ?? throw new NotFoundException(nameof(Enrollment), request.EnrollmentId);

            if (enrollment.Status != EnrollmentStatus.Confirmed)
                throw new ConflictException("A payment plan can only be created for a confirmed enrollment.");

            if (enrollment.PaymentPlan is not null)
                throw new ConflictException("This enrollment already has a payment plan.");

            if (enrollment.AgreedPrice <= 0m)
                throw new ConflictException("The agreed price is zero, so no payment plan is needed.");

            var total = enrollment.AgreedPrice;
            var count = request.InstallmentsCount;
            var baseAmount = Math.Floor(total / count);
            var remainder = total - baseAmount * count;

            if (baseAmount < 1m)
                throw new ConflictException("The price is too small to be split into that many installments.");

            var plan = new PaymentPlan
            {
                EnrollmentId = enrollment.Id,
                Enrollment = enrollment,
                TotalAmount = total,
                InstallmentsCount = count,
                Status = PaymentPlanStatus.Open
            };

            for (var number = 1; number <= count; number++)
            {
                plan.Installments.Add(new Installment
                {
                    Number = number,
                    Amount = number == 1 ? baseAmount + remainder : baseAmount,
                    DueDate = request.FirstDueDate.AddMonths(number - 1),
                    Status = InstallmentStatus.Unpaid
                });
            }

            await uow.PaymentPlans.AddAsync(plan, ct);
            await uow.SaveChangesAsync(ct);

            await audit.LogAsync(
                "PaymentPlan.Created", nameof(PaymentPlan), plan.Id,
                oldValues: null,
                newValues: new { plan.EnrollmentId, plan.TotalAmount, plan.InstallmentsCount, request.FirstDueDate },
                reason: null, ct);
            await uow.SaveChangesAsync(ct);

            return plan.ToDto(Today());
        }, ct);
    }

    public async Task<PaymentPlanDto> GetByEnrollmentAsync(int enrollmentId, CancellationToken ct = default)
    {
        var plan = await uow.PaymentPlans.GetByEnrollmentIdAsync(enrollmentId, ct)
            ?? throw new NotFoundException("Payment plan of enrollment", enrollmentId);
        return plan.ToDto(Today());
    }

    private DateOnly Today() => DateOnly.FromDateTime(clock.UtcNow);
}
