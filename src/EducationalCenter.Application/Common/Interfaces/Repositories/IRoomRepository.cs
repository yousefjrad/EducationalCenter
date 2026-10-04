using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Common.Interfaces.Repositories;

public interface IRoomRepository : IRepository<Room>
{
    Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken ct = default);

    /// <summary>True if the room is referenced by any section or class session.</summary>
    Task<bool> IsInUseAsync(int roomId, CancellationToken ct = default);

    Task<(IReadOnlyList<Room> Items, int TotalCount)> SearchAsync(
        string? search, bool? isActive, int page, int pageSize, CancellationToken ct = default);

    /// <summary>The room with that name (case-insensitive), or null.</summary>
    Task<Room?> GetByNameAsync(string name, CancellationToken ct = default);
}
