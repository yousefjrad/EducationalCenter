namespace EducationalCenter.Application.Features.Certificates;

public sealed record CertificateDto(
    int Id,
    int EnrollmentId,
    int StudentId,
    string StudentName,
    string CourseName,
    string SectionName,
    string CertificateNumber,
    DateTime IssuedAt,
    bool IsOverride,
    string? OverrideReason);

public sealed record CertificateEligibilityDto(
    int EnrollmentId,
    bool AlreadyIssued,
    bool IsEligible,
    IReadOnlyList<string> Blockers);

/// <summary>Issues the certificate even though the normal conditions are not met (Admin only, via API policy).</summary>
public sealed record IssueCertificateOverrideRequest(string Reason);

public sealed record CertificateListQuery(int? StudentId = null, int? SectionId = null, int Page = 1, int PageSize = 20);

public sealed record CertificateFileDto(byte[] Content, string FileName);
