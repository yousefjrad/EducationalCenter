using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Common.Interfaces.Repositories;

/// <summary>Append-only: there is no update or delete.</summary>
public interface IAuditLogRepository
{
    Task AddAsync(AuditLog entry, CancellationToken ct = default);

    /// <summary>
    /// Items come with User loaded, newest first. <paramref name="from"/> and <paramref name="to"/>
    /// filter on the UTC date of CreatedAt (inclusive).
    /// </summary>
    Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> SearchAsync(
        int? userId, string? entityName, int? entityId, string? action, DateOnly? from, DateOnly? to,
        int page, int pageSize, CancellationToken ct = default);
}
