using EducationalCenter.Domain.Enums;
using FluentValidation;

namespace EducationalCenter.Application.Features.Trainers;

public sealed class CreateTrainerRequestValidator : AbstractValidator<CreateTrainerRequest>
{
    public CreateTrainerRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(20).Matches(@"^\+?[0-9\s\-()]{6,20}$");
        RuleFor(x => x.Specialty).MaximumLength(100);
        RuleFor(x => x.PayType).IsInEnum();
        RuleFor(x => x.PayCurrency).IsInEnum();
        RuleFor(x => x.PayValue).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.PayValue).LessThanOrEqualTo(100m).When(x => x.PayType == TrainerPayType.Percentage);
    }
}

public sealed class UpdateTrainerRequestValidator : AbstractValidator<UpdateTrainerRequest>
{
    public UpdateTrainerRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(20).Matches(@"^\+?[0-9\s\-()]{6,20}$");
        RuleFor(x => x.Specialty).MaximumLength(100);
        RuleFor(x => x.PayType).IsInEnum();
        RuleFor(x => x.PayCurrency).IsInEnum();
        RuleFor(x => x.PayValue).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.PayValue).LessThanOrEqualTo(100m).When(x => x.PayType == TrainerPayType.Percentage);
    }
}

public sealed class TrainerListQueryValidator : AbstractValidator<TrainerListQuery>
{
    public TrainerListQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
