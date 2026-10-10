using Asp.Versioning;
using EducationalCenter.Application.Features.OnlinePayments;
using Microsoft.AspNetCore.Mvc;

namespace EducationalCenter.API.Controllers;

/// <summary>A signed-in student pays their own installments online.</summary>
[ApiVersion("1.0")]
public sealed class OnlinePaymentsController(IOnlinePaymentService service) : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(OnlinePaymentDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Start(StartOnlinePaymentRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await service.StartAsync(request, ct));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OnlinePaymentDto>>> Mine(CancellationToken ct) =>
        Ok(await service.ListMineAsync(ct));

    /// <summary>Development only (test gateway): confirms the payment.</summary>
    [HttpPost("{reference}/simulate-success")]
    public async Task<ActionResult<OnlinePaymentDto>> SimulateSuccess(string reference, CancellationToken ct) =>
        Ok(await service.SimulateSuccessAsync(reference, ct));
}