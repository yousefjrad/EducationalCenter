using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Common.Interfaces.Repositories;

public interface ICertificateRepository : IRepository<Certificate>
{
    /// <summary>Loads Enrollment with Student and Section (with Course).</summary>
    Task<Certificate?> GetWithDetailsAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Next sequence number for certificate numbers of that year (highest issued so far + 1, starting at 1).
    /// Must also consider soft-deleted certificates. A unique index on CertificateNumber is the final guard.
    /// </summary>
    Task<int> GetNextSequenceAsync(int year, CancellationToken ct = default);

    /// <summary>Items come with Enrollment (Student, Section with Course) loaded, newest first.</summary>
    Task<(IReadOnlyList<Certificate> Items, int TotalCount)> SearchAsync(
        int? studentId, int? sectionId, int page, int pageSize, CancellationToken ct = default);
}
