namespace EducationalCenter.Application.Features.CertificateTemplates;

public sealed record CertificateTemplateDto(
    int Id,
    string CenterName,
    string? LogoPath,
    string SignerName,
    string SignerTitle,
    string BodyText,
    string Language,
    bool IsDefault);

/// <param name="Language">"ar" or "en". The first template of a language automatically becomes its default.</param>
/// <param name="BodyText">
/// May contain: {StudentName}, {CourseName}, {SectionName}, {CertificateNumber}, {IssueDate}, {CenterName}.
/// </param>
public sealed record CreateCertificateTemplateRequest(
    string CenterName,
    string? LogoPath,
    string SignerName,
    string SignerTitle,
    string BodyText,
    string Language,
    bool IsDefault);

/// <remarks>The language cannot change after creation.</remarks>
public sealed record UpdateCertificateTemplateRequest(
    string CenterName,
    string? LogoPath,
    string SignerName,
    string SignerTitle,
    string BodyText,
    bool IsDefault);
