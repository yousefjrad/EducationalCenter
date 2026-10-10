using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Common.Models;
using EducationalCenter.Application.Features.PaymentPlans;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Enums;
using EducationalCenter.Domain.Exceptions;

namespace EducationalCenter.Application.Features.Payments;

public sealed class PaymentService(
    IUnitOfWork uow,
    IClock clock,
    ICurrentUser currentUser,
    IReceiptNumberGenerator receiptNumbers,
    IAuditLogger audit) : IPaymentService
{
    public async Task<PaymentDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var payment = await uow.Payments.GetWithDetailsAsync(id, ct)
            ?? throw new NotFoundException(nameof(Payment), id);
        return payment.ToDto();
    }

    public async Task<PagedResult<PaymentDto>> ListAsync(PaymentListQuery query, CancellationToken ct = default)
    {
        var (items, total) = await uow.Payments.SearchAsync(
            query.EnrollmentId, query.StudentId, query.From, query.To, query.Status,
            query.Page, query.PageSize, ct);

        return new PagedResult<PaymentDto>(items.Select(p => p.ToDto()).ToList(), total, query.Page, query.PageSize);
    }

    public Task<PaymentDto> RecordAsync(RecordPaymentRequest request, CancellationToken ct = default) =>
        RecordForUserAsync(request, currentUser.RequireUserId(), ct);

    public Task<PaymentDto> RecordForUserAsync(RecordPaymentRequest request, int userId, CancellationToken ct = default)
    {
        return uow.ExecuteInTransactionAsync(async () =>
        {

            var plan = await uow.PaymentPlans.GetByInstallmentIdAsync(request.InstallmentId, ct)
                ?? throw new NotFoundException("Installment", request.InstallmentId);

            if (plan.Status != PaymentPlanStatus.Open)
                throw new ConflictException("The payment plan is not open for payments.");

            var installment = plan.Installments.First(i => i.Id == request.InstallmentId);
            var paidSoFar = PaymentMath.PaidSyp(installment);
            var remaining = installment.Amount - paidSoFar;

            if (remaining <= 0m)
                throw new ConflictException("This installment is already fully paid.");

            // The exchange rate and the SYP equivalent are frozen at entry time.
            var amountInSyp = request.Currency == Currency.Usd
                ? Math.Round(request.AmountPaid * request.ExchangeRate!.Value, 0, MidpointRounding.AwayFromZero)
                : request.AmountPaid;

            if (amountInSyp <= 0m)
                throw new ConflictException("The amount is too small.");

            if (amountInSyp > remaining)
                throw new ConflictException(
                    $"The payment ({amountInSyp:0.##} SYP) exceeds the installment's remaining balance ({remaining:0.##} SYP).");

            var now = clock.UtcNow;

            var payment = new Payment
            {
                InstallmentId = installment.Id,
                Installment = installment,
                Currency = request.Currency,
                AmountPaid = request.AmountPaid,
                ExchangeRate = request.Currency == Currency.Usd ? request.ExchangeRate : null,
                AmountInSyp = amountInSyp,
                PaidAt = now,
                ReceivedByUserId = userId,
                Status = PaymentStatus.Valid
            };

            var receipt = new Receipt
            {
                Payment = payment,
                ReceiptNumber = await receiptNumbers.NextAsync(ct),
                IssuedAt = now,
                IssuedByUserId = userId
            };
            payment.Receipt = receipt;

            // Keep the stored statuses in step with the new payment.
            var paidAfter = paidSoFar + amountInSyp;
            installment.Status = PaymentMath.StoredStatus(paidAfter, installment.Amount);

            var allPaid = plan.Installments.All(i =>
                i.Id == installment.Id
                    ? paidAfter >= i.Amount
                    : PaymentMath.PaidSyp(i) >= i.Amount);
            if (allPaid)
                plan.Status = PaymentPlanStatus.FullyPaid;

            await uow.Payments.AddAsync(payment, ct);
            await uow.Receipts.AddAsync(receipt, ct);
            await uow.SaveChangesAsync(ct);

            await audit.LogAsync(
                "Payment.Created", nameof(Payment), payment.Id,
                oldValues: null,
                newValues: new
                {
                    payment.InstallmentId,
                    payment.Currency,
                    payment.AmountPaid,
                    payment.ExchangeRate,
                    payment.AmountInSyp,
                    receipt.ReceiptNumber
                },
                reason: null, ct);
            await uow.SaveChangesAsync(ct);

            return payment.ToDto();
        }, ct);
    }

    public Task<PaymentDto> CancelAsync(int id, CancelPaymentRequest request, CancellationToken ct = default)
    {
        return uow.ExecuteInTransactionAsync(async () =>
        {
            var payment = await uow.Payments.GetWithDetailsAsync(id, ct)
                ?? throw new NotFoundException(nameof(Payment), id);

            if (payment.Status != PaymentStatus.Valid)
                throw new ConflictException("The payment is already cancelled.");

            var plan = await uow.PaymentPlans.GetByInstallmentIdAsync(payment.InstallmentId, ct)
                ?? throw new NotFoundException("Installment", payment.InstallmentId);
            var installment = plan.Installments.First(i => i.Id == payment.InstallmentId);

            // Recompute the installment without the cancelled payment.
            var paidAfter = installment.Payments
                .Where(p => p.Status == PaymentStatus.Valid && p.Id != payment.Id)
                .Sum(p => p.AmountInSyp);

            payment.Status = PaymentStatus.Cancelled;
            payment.CancelReason = request.Reason.Trim();
            installment.Status = PaymentMath.StoredStatus(paidAfter, installment.Amount);

            if (plan.Status == PaymentPlanStatus.FullyPaid)
                plan.Status = PaymentPlanStatus.Open;

            await uow.SaveChangesAsync(ct);

            await audit.LogAsync(
                "Payment.Cancelled", nameof(Payment), payment.Id,
                oldValues: new { Status = PaymentStatus.Valid, payment.AmountInSyp },
                newValues: new { Status = PaymentStatus.Cancelled },
                reason: request.Reason, ct);
            await uow.SaveChangesAsync(ct);

            return payment.ToDto();
        }, ct);
    }
}
