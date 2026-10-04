namespace EducationalCenter.Application.Common.Models;

/// <summary>Everything the PDF generator needs; the body text already has its variables replaced.</summary>
public sealed record CertificatePdfModel(
    string CenterName,
    string? LogoPath,
    string SignerName,
    string SignerTitle,
    string Body,
    string CertificateNumber,
    DateOnly IssueDate,
    string Language);
