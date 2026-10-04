using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Common.Models;

namespace EducationalCenter.Application.Features.AuditLogs;

public sealed class AuditLogService(IUnitOfWork uow) : IAuditLogService
{
    public async Task<PagedResult<AuditLogDto>> ListAsync(AuditLogListQuery query, CancellationToken ct = default)
    {
        var (items, total) = await uow.AuditLogs.SearchAsync(
            query.UserId, query.EntityName?.Trim(), query.EntityId, query.Action?.Trim(),
            query.From, query.To, query.Page, query.PageSize, ct);

        return new PagedResult<AuditLogDto>(items.Select(a => a.ToDto()).ToList(), total, query.Page, query.PageSize);
    }
}
