using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Enums;

namespace EducationalCenter.Application.Common.Interfaces.Repositories;

public interface IWaitingListRepository : IRepository<WaitingListEntry>
{
    /// <summary>Tracked. Loads Student and Section.</summary>
    Task<WaitingListEntry?> GetWithDetailsAsync(int id, CancellationToken ct = default);

    /// <summary>Tracked. Entries with status Waiting for the section, ordered by Position.</summary>
    Task<IReadOnlyList<WaitingListEntry>> GetWaitingBySectionAsync(int sectionId, CancellationToken ct = default);

    /// <summary>Tracked. The student's Waiting entry for the section, if any.</summary>
    Task<WaitingListEntry?> GetWaitingEntryAsync(int studentId, int sectionId, CancellationToken ct = default);

    /// <summary>Items come with Student and Section loaded, ordered by section then position.</summary>
    Task<(IReadOnlyList<WaitingListEntry> Items, int TotalCount)> SearchAsync(
        int? sectionId, int? studentId, WaitingListStatus? status,
        int page, int pageSize, CancellationToken ct = default);
}
