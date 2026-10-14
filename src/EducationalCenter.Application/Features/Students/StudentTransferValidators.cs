using FluentValidation;

namespace EducationalCenter.Application.Features.Students;

public sealed class TransferStudentRequestValidator : AbstractValidator<TransferStudentRequest>
{
    public TransferStudentRequestValidator()
    {
        RuleFor(x => x.TargetBranchId).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}