using FluentValidation;

namespace EducationalCenter.Application.Features.Certificates;

public sealed class IssueCertificateOverrideRequestValidator : AbstractValidator<IssueCertificateOverrideRequest>
{
    public IssueCertificateOverrideRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public sealed class CertificateListQueryValidator : AbstractValidator<CertificateListQuery>
{
    public CertificateListQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
