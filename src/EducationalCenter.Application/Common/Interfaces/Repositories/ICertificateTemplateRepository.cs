using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Common.Interfaces.Repositories;

public interface ICertificateTemplateRepository : IRepository<CertificateTemplate>
{
    /// <summary>Tracked. All templates of the language.</summary>
    Task<IReadOnlyList<CertificateTemplate>> GetByLanguageAsync(string language, CancellationToken ct = default);

    /// <summary>The default template of the language, or null.</summary>
    Task<CertificateTemplate?> GetDefaultAsync(string language, CancellationToken ct = default);
}
