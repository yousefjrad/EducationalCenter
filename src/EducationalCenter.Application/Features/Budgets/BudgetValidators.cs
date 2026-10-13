using FluentValidation;

namespace EducationalCenter.Application.Features.Budgets;

public sealed class CreateBudgetRequestValidator : AbstractValidator<CreateBudgetRequest>
{
    public CreateBudgetRequestValidator()
    {
        RuleFor(x => x.Month).Must(d => d != default).WithMessage("'Month' is required.");
        RuleFor(x => x.RequestedAmountSyp).GreaterThan(0m).PrecisionScale(14, 2, true);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public sealed class UpdateBudgetRequestValidator : AbstractValidator<UpdateBudgetRequest>
{
    public UpdateBudgetRequestValidator()
    {
        RuleFor(x => x.RequestedAmountSyp).GreaterThan(0m).PrecisionScale(14, 2, true);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public sealed class ApproveBudgetRequestValidator : AbstractValidator<ApproveBudgetRequest>
{
    public ApproveBudgetRequestValidator()
    {
        RuleFor(x => x.ApprovedAmountSyp).Must(a => a is null || a > 0m).WithMessage("'Approved Amount' must be greater than zero.");
    }
}

public sealed class RejectBudgetRequestValidator : AbstractValidator<RejectBudgetRequest>
{
    public RejectBudgetRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}

public sealed class BudgetListQueryValidator : AbstractValidator<BudgetListQuery>
{
    public BudgetListQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}