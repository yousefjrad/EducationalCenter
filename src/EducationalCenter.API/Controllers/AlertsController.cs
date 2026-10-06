using Asp.Versioning;
using EducationalCenter.Application.Features.Alerts;
using Microsoft.AspNetCore.Mvc;

namespace EducationalCenter.API.Controllers;

[ApiVersion("1.0")]
public sealed class AlertsController(IAlertService alerts) : ApiControllerBase
{
    /// <summary>
    /// The front desk's to-do list. Any signed-in user may call it; each part is filled only if the
    /// user's role may see that data, otherwise it is null.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<AlertsDto>> Get(CancellationToken ct) =>
        Ok(await alerts.GetAlertsAsync(ct));
}
