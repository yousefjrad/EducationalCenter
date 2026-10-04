using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Common.Interfaces.Repositories;

public interface ICourseRepository : IRepository<Course>
{
    Task<bool> CodeExistsAsync(string code, int? excludeId, CancellationToken ct = default);
    Task<bool> HasSectionsAsync(int courseId, CancellationToken ct = default);

    Task<(IReadOnlyList<Course> Items, int TotalCount)> SearchAsync(
        string? search, bool? isActive, int page, int pageSize, CancellationToken ct = default);

    /// <summary>The course with that code (codes are stored upper-case), or null.</summary>
    Task<Course?> GetByCodeAsync(string code, CancellationToken ct = default);
}
