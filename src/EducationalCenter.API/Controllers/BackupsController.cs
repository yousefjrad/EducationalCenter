using Asp.Versioning;
using EducationalCenter.API.Authorization;
using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Common.Models;
using EducationalCenter.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace EducationalCenter.API.Controllers;

[ApiVersion("1.0")]
public sealed class BackupsController(IBackupService service) : ApiControllerBase
{
    /// <summary>The most recent backups of this database recorded by SQL Server, newest first (1 to 100).</summary>
    [HttpGet]
    [HasPermission(Permissions.Backups.View)]
    public async Task<ActionResult<IReadOnlyList<BackupInfo>>> List([FromQuery] int take = 20, CancellationToken ct = default) =>
        Ok(await service.ListBackupsAsync(Math.Clamp(take, 1, 100), ct));

    /// <summary>Takes a full backup now and verifies it. Large databases can take a while.</summary>
    [HttpPost]
    [HasPermission(Permissions.Backups.Create)]
    public async Task<ActionResult<BackupInfo>> Create(CancellationToken ct) =>
        Ok(await service.CreateBackupAsync(ct));
}
