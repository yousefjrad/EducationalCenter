using FluentValidation;

namespace EducationalCenter.Application.Features.Students;

public sealed class CreateStudentRequestValidator : AbstractValidator<CreateStudentRequest>
{
    public CreateStudentRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(20).Matches(@"^\+?[0-9\s\-()]{6,20}$");
    }
}

public sealed class UpdateStudentRequestValidator : AbstractValidator<UpdateStudentRequest>
{
    public UpdateStudentRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(20).Matches(@"^\+?[0-9\s\-()]{6,20}$");
    }
}

public sealed class StudentListQueryValidator : AbstractValidator<StudentListQuery>
{
    public StudentListQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
