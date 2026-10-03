using FluentValidation;

namespace EducationalCenter.Application.Features.WaitingList;

public sealed class AddToWaitingListRequestValidator : AbstractValidator<AddToWaitingListRequest>
{
    public AddToWaitingListRequestValidator()
    {
        RuleFor(x => x.StudentId).GreaterThan(0);
        RuleFor(x => x.SectionId).GreaterThan(0);
    }
}

public sealed class WaitingListQueryValidator : AbstractValidator<WaitingListQuery>
{
    public WaitingListQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
