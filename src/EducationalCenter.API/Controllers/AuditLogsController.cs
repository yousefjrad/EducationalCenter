using Asp.Versioning;
using EducationalCenter.API.Authorization;
using EducationalCenter.Application.Common.Models;
using EducationalCenter.Application.Features.AuditLogs;
using EducationalCenter.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace EducationalCenter.API.Controllers;

[ApiVersion("1.0")]
public sealed class AuditLogsController(IAuditLogService service) : ApiControllerBase
{
    /// <summary>Read-only. Entries are written by the financial services.</summary>
    [HttpGet]
    [HasPermission(Permissions.AuditLog.View)]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> List([FromQuery] AuditLogListQuery query, CancellationToken ct) =>
        Ok(await service.ListAsync(query, ct));
}
