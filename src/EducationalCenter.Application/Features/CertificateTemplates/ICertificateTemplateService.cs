namespace EducationalCenter.Application.Features.CertificateTemplates;

public interface ICertificateTemplateService
{
    Task<CertificateTemplateDto> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>All templates, or only those of one language ("ar" / "en").</summary>
    Task<IReadOnlyList<CertificateTemplateDto>> ListAsync(string? language = null, CancellationToken ct = default);

    Task<CertificateTemplateDto> CreateAsync(CreateCertificateTemplateRequest request, CancellationToken ct = default);
    Task<CertificateTemplateDto> UpdateAsync(int id, UpdateCertificateTemplateRequest request, CancellationToken ct = default);
    Task<CertificateTemplateDto> SetDefaultAsync(int id, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
