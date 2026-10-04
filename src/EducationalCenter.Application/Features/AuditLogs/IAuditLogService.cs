using EducationalCenter.Application.Common.Models;

namespace EducationalCenter.Application.Features.AuditLogs;

/// <summary>Read-only. Entries are written by the financial services through IAuditLogger.</summary>
public interface IAuditLogService
{
    Task<PagedResult<AuditLogDto>> ListAsync(AuditLogListQuery query, CancellationToken ct = default);
}
