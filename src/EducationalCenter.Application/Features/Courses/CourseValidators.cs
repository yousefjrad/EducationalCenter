using FluentValidation;

namespace EducationalCenter.Application.Features.Courses;

internal static class CourseRuleExtensions
{
    public static IRuleBuilderOptions<T, string> ValidCode<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .MaximumLength(20)
            .Matches("^[A-Za-z0-9_-]+$")
            .WithMessage("'{PropertyName}' may contain only letters, digits, '-' and '_'.");

    public static IRuleBuilderOptions<T, string> ValidName<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MaximumLength(150);

    public static IRuleBuilderOptions<T, int> ValidDuration<T>(this IRuleBuilder<T, int> rule) =>
        rule.InclusiveBetween(1, 10_000);

    public static IRuleBuilderOptions<T, decimal> ValidPrice<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.GreaterThanOrEqualTo(0m);
}

public sealed class CreateCourseRequestValidator : AbstractValidator<CreateCourseRequest>
{
    public CreateCourseRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName();
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.Level).MaximumLength(50);
        RuleFor(x => x.DefaultDurationHours).ValidDuration();
        RuleFor(x => x.DefaultPrice).ValidPrice();
    }
}

public sealed class UpdateCourseRequestValidator : AbstractValidator<UpdateCourseRequest>
{
    public UpdateCourseRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName();
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.Level).MaximumLength(50);
        RuleFor(x => x.DefaultDurationHours).ValidDuration();
        RuleFor(x => x.DefaultPrice).ValidPrice();
    }
}

public sealed class CourseListQueryValidator : AbstractValidator<CourseListQuery>
{
    public CourseListQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
