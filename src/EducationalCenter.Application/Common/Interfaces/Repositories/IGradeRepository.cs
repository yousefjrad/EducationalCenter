using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Common.Interfaces.Repositories;

public interface IGradeRepository : IRepository<Grade>
{
    /// <summary>Loads Enrollment with its Student and Section.</summary>
    Task<Grade?> GetByEnrollmentAsync(int enrollmentId, CancellationToken ct = default);

    /// <summary>Loads Enrollment with Student and Section; ordered by student name.</summary>
    Task<IReadOnlyList<Grade>> GetBySectionAsync(int sectionId, CancellationToken ct = default);
}
