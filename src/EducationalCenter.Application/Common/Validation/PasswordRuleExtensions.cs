using FluentValidation;

namespace EducationalCenter.Application.Common.Validation;

internal static class PasswordRuleExtensions
{
    /// <summary>At least 8 characters with at least one letter and one digit.</summary>
    public static IRuleBuilderOptions<T, string> ValidPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .MinimumLength(8)
            .MaximumLength(100)
            .Matches("[A-Za-z]").WithMessage("'{PropertyName}' must contain at least one letter.")
            .Matches("[0-9]").WithMessage("'{PropertyName}' must contain at least one digit.");
}
