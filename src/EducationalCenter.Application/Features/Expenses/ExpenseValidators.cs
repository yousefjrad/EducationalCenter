using EducationalCenter.Domain.Enums;
using FluentValidation;

namespace EducationalCenter.Application.Features.Expenses;

internal static class ExpenseRuleExtensions
{
    public static void ApplyMoneyRules<T>(
        this AbstractValidator<T> validator,
        System.Linq.Expressions.Expression<Func<T, Currency>> currency,
        System.Linq.Expressions.Expression<Func<T, decimal>> amount,
        System.Linq.Expressions.Expression<Func<T, decimal?>> rate)
    {
        var currencyFunc = currency.Compile();
        var amountFunc = amount.Compile();

        validator.RuleFor(currency).IsInEnum();
        validator.RuleFor(amount).GreaterThan(0m).PrecisionScale(14, 2, true);

        validator.RuleFor(amount)
            .Must(a => a == Math.Truncate(a))
            .When(x => currencyFunc(x) == Currency.Syp)
            .WithMessage("Amounts in SYP must be whole numbers.");

        validator.RuleFor(rate)
            .NotNull().WithMessage("'Exchange Rate' is required for USD amounts.")
            .Must(r => r is > 0m and <= 1_000_000m).WithMessage("'Exchange Rate' must be between 0 and 1,000,000.")
            .When(x => currencyFunc(x) == Currency.Usd);

        validator.RuleFor(rate)
            .Null().WithMessage("'Exchange Rate' must be empty for SYP amounts.")
            .When(x => currencyFunc(x) == Currency.Syp);
    }
}

public sealed class CreateExpenseRequestValidator : AbstractValidator<CreateExpenseRequest>
{
    public CreateExpenseRequestValidator()
    {
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ExpenseDate).Must(d => d != default).WithMessage("'Expense Date' is required.");
        this.ApplyMoneyRules(x => x.Currency, x => x.Amount, x => x.ExchangeRate);
    }
}

public sealed class UpdateExpenseRequestValidator : AbstractValidator<UpdateExpenseRequest>
{
    public UpdateExpenseRequestValidator()
    {
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ExpenseDate).Must(d => d != default).WithMessage("'Expense Date' is required.");
        this.ApplyMoneyRules(x => x.Currency, x => x.Amount, x => x.ExchangeRate);
    }
}

public sealed class ExpenseListQueryValidator : AbstractValidator<ExpenseListQuery>
{
    public ExpenseListQueryValidator()
    {
        RuleFor(x => x.Category).MaximumLength(100);
        RuleFor(x => x.To)
            .Must((q, to) => to is null || q.From is null || to >= q.From)
            .WithMessage("'To' must be on or after 'From'.");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
