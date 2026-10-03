using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Enums;

namespace EducationalCenter.Application.Common.Interfaces.Repositories;

public interface IClassSessionRepository : IRepository<ClassSession>
{
    /// <summary>Loads Section, Room and Trainer.</summary>
    Task<ClassSession?> GetWithDetailsAsync(int id, CancellationToken ct = default);

    /// <summary>Ordered by date then start time; Section, Room and Trainer loaded.</summary>
    Task<IReadOnlyList<ClassSession>> GetBySectionAsync(int sectionId, CancellationToken ct = default);

    /// <summary>
    /// True if the room has a session overlapping the range on that date.
    /// Only sessions with status Scheduled or Held occupy a slot (Cancelled and Postponed do not).
    /// Overlap means: start &lt; otherEnd AND end &gt; otherStart.
    /// </summary>
    Task<bool> HasRoomConflictAsync(
        int roomId, DateOnly date, TimeOnly start, TimeOnly end, int? excludeSessionId, CancellationToken ct = default);

    /// <summary>Same rule as <see cref="HasRoomConflictAsync"/>, for the trainer.</summary>
    Task<bool> HasTrainerConflictAsync(
        int trainerId, DateOnly date, TimeOnly start, TimeOnly end, int? excludeSessionId, CancellationToken ct = default);

    /// <summary>Items come with Section, Room and Trainer loaded, ordered by date then start time.</summary>
    Task<(IReadOnlyList<ClassSession> Items, int TotalCount)> SearchAsync(
        int? sectionId, int? roomId, int? trainerId, DateOnly? from, DateOnly? to, ClassSessionStatus? status,
        int page, int pageSize, CancellationToken ct = default);
}
