using EducationalCenter.Domain.Enums;
using FluentValidation;

namespace EducationalCenter.Application.Features.Payments;

public sealed class RecordPaymentRequestValidator : AbstractValidator<RecordPaymentRequest>
{
    public RecordPaymentRequestValidator()
    {
        RuleFor(x => x.InstallmentId).GreaterThan(0);
        RuleFor(x => x.Currency).IsInEnum();
        RuleFor(x => x.AmountPaid).GreaterThan(0m).PrecisionScale(14, 2, true);

        RuleFor(x => x.AmountPaid)
            .Must(a => a == Math.Truncate(a))
            .When(x => x.Currency == Currency.Syp)
            .WithMessage("Amounts in SYP must be whole numbers.");

        RuleFor(x => x.ExchangeRate)
            .NotNull().WithMessage("'Exchange Rate' is required for USD payments.")
            .Must(r => r is > 0m and <= 1_000_000m).WithMessage("'Exchange Rate' must be between 0 and 1,000,000.")
            .When(x => x.Currency == Currency.Usd);

        RuleFor(x => x.ExchangeRate)
            .Null().WithMessage("'Exchange Rate' must be empty for SYP payments.")
            .When(x => x.Currency == Currency.Syp);
    }
}

public sealed class CancelPaymentRequestValidator : AbstractValidator<CancelPaymentRequest>
{
    public CancelPaymentRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public sealed class PaymentListQueryValidator : AbstractValidator<PaymentListQuery>
{
    public PaymentListQueryValidator()
    {
        RuleFor(x => x.To)
            .Must((q, to) => to is null || q.From is null || to >= q.From)
            .WithMessage("'To' must be on or after 'From'.");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
