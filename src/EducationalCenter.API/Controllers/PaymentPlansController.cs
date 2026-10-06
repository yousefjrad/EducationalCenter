using Asp.Versioning;
using EducationalCenter.API.Authorization;
using EducationalCenter.Application.Features.PaymentPlans;
using EducationalCenter.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace EducationalCenter.API.Controllers;

[ApiVersion("1.0")]
public sealed class PaymentPlansController(IPaymentPlanService service) : ApiControllerBase
{
    /// <summary>Splits the enrollment's agreed price into equal monthly installments (1 = full payment, up to 24).</summary>
    [HttpPost]
    [HasPermission(Permissions.PaymentPlans.Create)]
    public async Task<ActionResult<PaymentPlanDto>> Create(CreatePaymentPlanRequest request, CancellationToken ct)
    {
        var dto = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetByEnrollment), new { enrollmentId = dto.EnrollmentId, version = "1.0" }, dto);
    }

    [HttpGet("enrollments/{enrollmentId:int}")]
    [HasPermission(Permissions.PaymentPlans.View)]
    public async Task<ActionResult<PaymentPlanDto>> GetByEnrollment(int enrollmentId, CancellationToken ct) =>
        Ok(await service.GetByEnrollmentAsync(enrollmentId, ct));
}
