using FluentValidation;

namespace EducationalCenter.Application.Features.Grades;

public sealed class RecordGradeRequestValidator : AbstractValidator<RecordGradeRequest>
{
    public RecordGradeRequestValidator()
    {
        RuleFor(x => x.EnrollmentId).GreaterThan(0);
        RuleFor(x => x.MaxScore).GreaterThan(0m).LessThanOrEqualTo(1000m);
        RuleFor(x => x.Score).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.Score).LessThanOrEqualTo(x => x.MaxScore)
            .WithMessage("'Score' must not exceed 'Max Score'.");
    }
}
