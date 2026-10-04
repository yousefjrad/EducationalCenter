using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Features.CertificateTemplates;

public static class CertificateTemplateMappings
{
    public static CertificateTemplateDto ToDto(this CertificateTemplate t) => new(
        t.Id, t.CenterName, t.LogoPath, t.SignerName, t.SignerTitle, t.BodyText, t.Language, t.IsDefault);
}
