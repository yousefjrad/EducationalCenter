using Asp.Versioning;
using EducationalCenter.API.Authorization;
using EducationalCenter.Application.Features.Receipts;
using EducationalCenter.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace EducationalCenter.API.Controllers;

[ApiVersion("1.0")]
public sealed class ReceiptsController(IReceiptService service) : ApiControllerBase
{
    [HttpGet("{id:int}")]
    [HasPermission(Permissions.Receipts.View)]
    public async Task<ActionResult<ReceiptDto>> GetById(int id, CancellationToken ct) =>
        Ok(await service.GetByIdAsync(id, ct));

    [HttpGet("payments/{paymentId:int}")]
    [HasPermission(Permissions.Receipts.View)]
    public async Task<ActionResult<ReceiptDto>> GetByPayment(int paymentId, CancellationToken ct) =>
        Ok(await service.GetByPaymentAsync(paymentId, ct));

    /// <param name="language">"ar" or "en".</param>
    [HttpGet("{id:int}/pdf")]
    [HasPermission(Permissions.Receipts.View)]
    public async Task<IActionResult> GetPdf(int id, [FromQuery] string language = "ar", CancellationToken ct = default)
    {
        var file = await service.GetPdfAsync(id, language, ct);
        return File(file.Content, "application/pdf", file.FileName);
    }
}
