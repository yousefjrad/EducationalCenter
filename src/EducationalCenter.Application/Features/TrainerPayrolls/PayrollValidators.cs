using FluentValidation;

namespace EducationalCenter.Application.Features.TrainerPayrolls;

public sealed class PayrollRequestValidator : AbstractValidator<PayrollRequest>
{
    public PayrollRequestValidator()
    {
        RuleFor(x => x.TrainerId).GreaterThan(0);
        RuleFor(x => x.PeriodStart).Must(d => d != default).WithMessage("'Period Start' is required.");
        RuleFor(x => x.PeriodEnd).GreaterThanOrEqualTo(x => x.PeriodStart);
        RuleFor(x => x.PeriodEnd)
            .Must((x, end) => end.DayNumber - x.PeriodStart.DayNumber <= 366)
            .WithMessage("A payroll period cannot exceed 366 days.");
        RuleFor(x => x.ExchangeRate)
            .Must(r => r is null || r is > 0m and <= 1_000_000m)
            .WithMessage("'Exchange Rate' must be between 0 and 1,000,000.");
    }
}

public sealed class PayPayrollRequestValidator : AbstractValidator<PayPayrollRequest>
{
    public PayPayrollRequestValidator()
    {
        RuleFor(x => x.ExchangeRate)
            .Must(r => r is null || r is > 0m and <= 1_000_000m)
            .WithMessage("'Exchange Rate' must be between 0 and 1,000,000.");
    }
}

public sealed class CancelPayrollRequestValidator : AbstractValidator<CancelPayrollRequest>
{
    public CancelPayrollRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public sealed class PayrollListQueryValidator : AbstractValidator<PayrollListQuery>
{
    public PayrollListQueryValidator()
    {
        RuleFor(x => x.To)
            .Must((q, to) => to is null || q.From is null || to >= q.From)
            .WithMessage("'To' must be on or after 'From'.");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
