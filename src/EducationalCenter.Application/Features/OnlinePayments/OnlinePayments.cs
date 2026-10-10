using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Features.PaymentPlans;
using EducationalCenter.Application.Features.Payments;
using EducationalCenter.Application.Features.StudentPortal;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Enums;
using EducationalCenter.Domain.Exceptions;
using FluentValidation;

namespace EducationalCenter.Application.Features.OnlinePayments;

/// <param name="Amount">Optional: defaults to the installment's whole remaining balance. Whole SYP, never more than the remaining balance.</param>
public sealed record StartOnlinePaymentRequest(int InstallmentId, decimal? Amount);

public sealed record OnlinePaymentDto(
    string Reference,
    int InstallmentId,
    decimal AmountInSyp,
    PaymentIntentStatus Status,
    string Provider,
    string? RedirectUrl,
    DateTime ExpiresAt,
    int? PaymentId,
    int? ReceiptId,
    string? FailureReason);

public sealed record PaymentCheckoutRequest(string Reference, decimal AmountInSyp, string Description);

/// <param name="RedirectUrl">Where the student pays at the provider (null for providers without a page).</param>
public sealed record GatewayCheckout(string? RedirectUrl, string? ProviderReference);

/// <summary>An online payment provider (for example Sham Cash). Only this interface changes when the real provider is plugged in.</summary>
public interface IPaymentGateway
{
    string Name { get; }
    Task<GatewayCheckout> CreateCheckoutAsync(PaymentCheckoutRequest request, CancellationToken ct = default);
}

public interface IPaymentIntentStore
{
    Task AddAsync(PaymentIntent intent, CancellationToken ct = default);
    Task<PaymentIntent?> FindByReferenceAsync(string reference, CancellationToken ct = default);
    Task<IReadOnlyList<PaymentIntent>> ListByUserAsync(int userId, CancellationToken ct = default);

    /// <summary>Pending to Processing, atomically. False when another caller already moved it.</summary>
    Task<bool> TryMarkProcessingAsync(int id, CancellationToken ct = default);
    Task MarkSucceededAsync(int id, int paymentId, DateTime at, CancellationToken ct = default);
    Task MarkFailedAsync(int id, string reason, CancellationToken ct = default);
    Task MarkExpiredAsync(int id, CancellationToken ct = default);
}

public interface IOnlinePaymentService
{
    Task<OnlinePaymentDto> StartAsync(StartOnlinePaymentRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<OnlinePaymentDto>> ListMineAsync(CancellationToken ct = default);

    /// <summary>Development only: confirms the payment as the test gateway would. Not found when a real gateway is installed.</summary>
    Task<OnlinePaymentDto> SimulateSuccessAsync(string reference, CancellationToken ct = default);
}

public sealed class StartOnlinePaymentRequestValidator : AbstractValidator<StartOnlinePaymentRequest>
{
    public StartOnlinePaymentRequestValidator()
    {
        RuleFor(x => x.InstallmentId).GreaterThan(0);
        RuleFor(x => x.Amount)
            .Must(a => a is > 0m && a == Math.Truncate(a!.Value))
            .When(x => x.Amount is not null)
            .WithMessage("'Amount' must be a whole number of SYP greater than zero.");
    }
}

public sealed class OnlinePaymentService(
    IUnitOfWork uow,
    IClock clock,
    ICurrentUser currentUser,
    IStudentPortalReader reader,
    IPaymentGateway gateway,
    IPaymentIntentStore store,
    IPaymentService payments) : IOnlinePaymentService
{
    private static readonly TimeSpan IntentLifetime = TimeSpan.FromMinutes(30);

    public async Task<OnlinePaymentDto> StartAsync(StartOnlinePaymentRequest request, CancellationToken ct = default)
    {
        var (userId, studentId) = await WhoAsync(ct);

        var plan = await uow.PaymentPlans.GetByInstallmentIdAsync(request.InstallmentId, ct);
        if (plan is null || plan.Enrollment.StudentId != studentId)
            throw new NotFoundException("Installment", request.InstallmentId);

        if (plan.Status != PaymentPlanStatus.Open)
            throw new ConflictException("The payment plan is not open for payments.");

        var installment = plan.Installments.First(i => i.Id == request.InstallmentId);
        var remaining = installment.Amount - PaymentMath.PaidSyp(installment);
        if (remaining <= 0m)
            throw new ConflictException("This installment is already fully paid.");

        var amount = request.Amount ?? remaining;
        if (amount > remaining)
            throw new ConflictException(
                $"The amount ({amount:0.##} SYP) exceeds the installment's remaining balance ({remaining:0.##} SYP).");

        var now = clock.UtcNow;
        var reference = "PAY-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();

        var checkout = await gateway.CreateCheckoutAsync(
            new PaymentCheckoutRequest(reference, amount, $"Installment {installment.Number} - {plan.Enrollment.Section.Name}"), ct);

        var intent = new PaymentIntent
        {
            InstallmentId = installment.Id,
            UserId = userId,
            Reference = reference,
            Provider = gateway.Name,
            ProviderReference = checkout.ProviderReference,
            RedirectUrl = checkout.RedirectUrl,
            AmountInSyp = amount,
            Status = PaymentIntentStatus.Pending,
            ExpiresAt = now.Add(IntentLifetime)
        };

        await store.AddAsync(intent, ct);
        return ToDto(intent);
    }

    public async Task<IReadOnlyList<OnlinePaymentDto>> ListMineAsync(CancellationToken ct = default)
    {
        var (userId, _) = await WhoAsync(ct);
        return (await store.ListByUserAsync(userId, ct)).Select(ToDto).ToList();
    }

    public async Task<OnlinePaymentDto> SimulateSuccessAsync(string reference, CancellationToken ct = default)
    {
        if (gateway.Name != "Fake")
            throw new NotFoundException("Payment", reference);

        var (userId, _) = await WhoAsync(ct);

        var intent = await store.FindByReferenceAsync(reference, ct);
        if (intent is null || intent.UserId != userId)
            throw new NotFoundException("Payment", reference);

        if (intent.ExpiresAt <= clock.UtcNow)
        {
            await store.MarkExpiredAsync(intent.Id, ct);
            throw new ConflictException("This payment request has expired. Start a new one.");
        }

        // Only one caller can move Pending to Processing, so the same request is never paid twice.
        if (!await store.TryMarkProcessingAsync(intent.Id, ct))
            throw new ConflictException("This payment request was already processed.");

        try
        {
            var payment = await payments.RecordForUserAsync(
                new RecordPaymentRequest(intent.InstallmentId, Currency.Syp, intent.AmountInSyp, null),
                intent.UserId, ct);

            await store.MarkSucceededAsync(intent.Id, payment.Id, clock.UtcNow, ct);

            return ToDto(intent) with
            {
                Status = PaymentIntentStatus.Succeeded,
                PaymentId = payment.Id,
                ReceiptId = payment.ReceiptId
            };
        }
        catch (Exception ex) when (ex is ConflictException or NotFoundException)
        {
            await store.MarkFailedAsync(intent.Id, ex.Message, ct);
            throw;
        }
    }

    private async Task<(int UserId, int StudentId)> WhoAsync(CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException("Sign in is required.");
        var studentId = await reader.FindStudentIdAsync(userId, ct)
            ?? throw new NotFoundException("No student profile is linked to this account.");
        return (userId, studentId);
    }

    private static OnlinePaymentDto ToDto(PaymentIntent i) => new(
        i.Reference, i.InstallmentId, i.AmountInSyp, i.Status, i.Provider, i.RedirectUrl,
        i.ExpiresAt, i.PaymentId, null, i.FailureReason);
}