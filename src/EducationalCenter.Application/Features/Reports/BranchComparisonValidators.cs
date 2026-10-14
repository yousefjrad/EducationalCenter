using FluentValidation;

namespace EducationalCenter.Application.Features.Reports;

public sealed class BranchComparisonQueryValidator : AbstractValidator<BranchComparisonQuery>
{
    public BranchComparisonQueryValidator()
    {
        RuleFor(x => x.To)
            .Must((q, to) => to is null || q.From is null || to >= q.From)
            .WithMessage("'To' must be on or after 'From'.");

        RuleFor(x => x.To)
            .Must((q, to) => to is null || q.From is null || to!.Value.DayNumber - q.From!.Value.DayNumber <= 366)
            .WithMessage("The period cannot be longer than one year.");
    }
}