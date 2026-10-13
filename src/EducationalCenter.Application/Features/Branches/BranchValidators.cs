using FluentValidation;

namespace EducationalCenter.Application.Features.Branches;

public sealed class CreateBranchRequestValidator : AbstractValidator<CreateBranchRequest>
{
    public CreateBranchRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Code).NotEmpty().Matches("^[A-Za-z0-9]{2,5}$").WithMessage("Code must be 2 to 5 letters or digits.");
        RuleFor(x => x.Governorate).MaximumLength(100);
        RuleFor(x => x.Address).MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(20);
    }
}

public sealed class UpdateBranchRequestValidator : AbstractValidator<UpdateBranchRequest>
{
    public UpdateBranchRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Governorate).MaximumLength(100);
        RuleFor(x => x.Address).MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(20);
    }
}