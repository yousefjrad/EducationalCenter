using Asp.Versioning;
using EducationalCenter.API.Authorization;
using EducationalCenter.Application.Features.Settings;
using EducationalCenter.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace EducationalCenter.API.Controllers;

[ApiVersion("1.0")]
public sealed class SettingsController(ISettingService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.Settings.View)]
    public async Task<ActionResult<IReadOnlyList<SettingDto>>> List(CancellationToken ct) =>
        Ok(await service.ListAsync(ct));

    [HttpGet("{key}")]
    [HasPermission(Permissions.Settings.View)]
    public async Task<ActionResult<SettingDto>> GetByKey(string key, CancellationToken ct) =>
        Ok(await service.GetByKeyAsync(key, ct));

    /// <summary>Only existing keys can be changed. Values are text; each key has its own rule.</summary>
    [HttpPut("{key}")]
    [HasPermission(Permissions.Settings.Update)]
    public async Task<ActionResult<SettingDto>> Update(string key, UpdateSettingRequest request, CancellationToken ct) =>
        Ok(await service.UpdateAsync(key, request, ct));
}
