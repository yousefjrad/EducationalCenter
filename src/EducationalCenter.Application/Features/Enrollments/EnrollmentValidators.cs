using FluentValidation;

namespace EducationalCenter.Application.Features.Enrollments;

public sealed class CreateEnrollmentRequestValidator : AbstractValidator<CreateEnrollmentRequest>
{
    public CreateEnrollmentRequestValidator()
    {
        RuleFor(x => x.StudentId).GreaterThan(0);
        RuleFor(x => x.SectionId).GreaterThan(0);
    }
}

public sealed class TransferEnrollmentRequestValidator : AbstractValidator<TransferEnrollmentRequest>
{
    public TransferEnrollmentRequestValidator()
    {
        RuleFor(x => x.TargetSectionId).GreaterThan(0);
    }
}

public sealed class EnrollmentListQueryValidator : AbstractValidator<EnrollmentListQuery>
{
    public EnrollmentListQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
