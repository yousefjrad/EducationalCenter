using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Enums;

namespace EducationalCenter.Application.Common.Interfaces.Repositories;

public interface ISectionRepository : IRepository<Section>
{
    /// <summary>Loads Course, Trainer, Room and Schedules (tracked, so it can be updated).</summary>
    Task<Section?> GetWithDetailsAsync(int id, CancellationToken ct = default);

    /// <summary>Section names are unique within a course.</summary>
    Task<bool> NameExistsAsync(int courseId, string name, int? excludeId, CancellationToken ct = default);

    /// <summary>True if the section has any class session (any status).</summary>
    Task<bool> HasSessionsAsync(int sectionId, CancellationToken ct = default);

    /// <summary>True if the section has any enrollment or waiting-list entry (any status).</summary>
    Task<bool> HasEnrollmentsAsync(int sectionId, CancellationToken ct = default);

    void RemoveSchedules(IEnumerable<SectionSchedule> schedules);

    /// <summary>Items come with Course, Trainer, Room and Schedules loaded.</summary>
    Task<(IReadOnlyList<Section> Items, int TotalCount)> SearchAsync(
        string? search, int? courseId, int? trainerId, int? roomId, SectionStatus? status,
        int page, int pageSize, CancellationToken ct = default);

    /// <summary>The section with that name inside the course (case-insensitive), or null.</summary>
    Task<Section?> GetByNameAsync(int courseId, string name, CancellationToken ct = default);
}
