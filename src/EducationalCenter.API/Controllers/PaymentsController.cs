using Asp.Versioning;
using EducationalCenter.API.Authorization;
using EducationalCenter.Application.Common.Models;
using EducationalCenter.Application.Features.Payments;
using EducationalCenter.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace EducationalCenter.API.Controllers;

[ApiVersion("1.0")]
public sealed class PaymentsController(IPaymentService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.Payments.View)]
    public async Task<ActionResult<PagedResult<PaymentDto>>> List([FromQuery] PaymentListQuery query, CancellationToken ct) =>
        Ok(await service.ListAsync(query, ct));

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.Payments.View)]
    public async Task<ActionResult<PaymentDto>> GetById(int id, CancellationToken ct) =>
        Ok(await service.GetByIdAsync(id, ct));

    /// <summary>Records a cash payment against one installment and issues its receipt. Overpaying is rejected.</summary>
    [HttpPost]
    [HasPermission(Permissions.Payments.Create)]
    public async Task<ActionResult<PaymentDto>> Record(RecordPaymentRequest request, CancellationToken ct)
    {
        var dto = await service.RecordAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id, version = "1.0" }, dto);
    }

    /// <summary>The payment is kept and marked Cancelled; it is never deleted.</summary>
    [HttpPost("{id:int}/cancel")]
    [HasPermission(Permissions.Payments.Cancel)]
    public async Task<ActionResult<PaymentDto>> Cancel(int id, CancelPaymentRequest request, CancellationToken ct) =>
        Ok(await service.CancelAsync(id, request, ct));
}
