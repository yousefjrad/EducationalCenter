using EducationalCenter.Application.Common.Models;

namespace EducationalCenter.Application.Features.Certificates;

public interface ICertificateService
{
    Task<CertificateDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PagedResult<CertificateDto>> ListAsync(CertificateListQuery query, CancellationToken ct = default);

    /// <summary>Why a certificate can or cannot be issued now (passing grade and full payment).</summary>
    Task<CertificateEligibilityDto> GetEligibilityAsync(int enrollmentId, CancellationToken ct = default);

    /// <summary>Normal issue: requires a passing grade and full payment.</summary>
    Task<CertificateDto> IssueAsync(int enrollmentId, CancellationToken ct = default);

    /// <summary>Issue with an Admin override; the reason is stored only if a condition was actually bypassed.</summary>
    Task<CertificateDto> IssueWithOverrideAsync(int enrollmentId, IssueCertificateOverrideRequest request, CancellationToken ct = default);

    /// <param name="language">"ar" or "en": picks that language's default template.</param>
    Task<CertificateFileDto> GetPdfAsync(int id, string language = "ar", CancellationToken ct = default);
}
