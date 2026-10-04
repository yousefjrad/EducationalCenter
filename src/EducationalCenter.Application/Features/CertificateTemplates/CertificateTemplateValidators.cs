using System.Text.RegularExpressions;
using FluentValidation;

namespace EducationalCenter.Application.Features.CertificateTemplates;

internal static class CertificateTemplateRuleExtensions
{
    private static readonly Regex VariablePattern = new(@"\{(\w+)\}", RegexOptions.Compiled);

    public static IRuleBuilderOptions<T, string> ValidBodyText<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .MaximumLength(2000)
            .Must(body => VariablePattern.Matches(body)
                .All(m => CertificateTemplateRules.AllowedVariables.Contains(m.Groups[1].Value)))
            .WithMessage("'Body Text' contains an unknown variable. Allowed: "
                         + string.Join(", ", CertificateTemplateRules.AllowedVariables.Select(v => "{" + v + "}")) + ".");
}

public sealed class CreateCertificateTemplateRequestValidator : AbstractValidator<CreateCertificateTemplateRequest>
{
    public CreateCertificateTemplateRequestValidator()
    {
        RuleFor(x => x.CenterName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.LogoPath).MaximumLength(500);
        RuleFor(x => x.SignerName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.SignerTitle).NotEmpty().MaximumLength(100);
        RuleFor(x => x.BodyText).ValidBodyText();
        RuleFor(x => x.Language).Must(l => CertificateTemplateRules.Languages.Contains(l))
            .WithMessage("'Language' must be 'ar' or 'en'.");
    }
}

public sealed class UpdateCertificateTemplateRequestValidator : AbstractValidator<UpdateCertificateTemplateRequest>
{
    public UpdateCertificateTemplateRequestValidator()
    {
        RuleFor(x => x.CenterName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.LogoPath).MaximumLength(500);
        RuleFor(x => x.SignerName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.SignerTitle).NotEmpty().MaximumLength(100);
        RuleFor(x => x.BodyText).ValidBodyText();
    }
}
