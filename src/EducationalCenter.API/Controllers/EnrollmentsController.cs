using Asp.Versioning;
using EducationalCenter.API.Authorization;
using EducationalCenter.Application.Common.Models;
using EducationalCenter.Application.Features.Enrollments;
using EducationalCenter.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace EducationalCenter.API.Controllers;

[ApiVersion("1.0")]
public sealed class EnrollmentsController(IEnrollmentService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.Enrollments.View)]
    public async Task<ActionResult<PagedResult<EnrollmentDto>>> List([FromQuery] EnrollmentListQuery query, CancellationToken ct) =>
        Ok(await service.ListAsync(query, ct));

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.Enrollments.View)]
    public async Task<ActionResult<EnrollmentDto>> GetById(int id, CancellationToken ct) =>
        Ok(await service.GetByIdAsync(id, ct));

    /// <summary>Direct enrollment (asHold = false) or a temporary seat hold (asHold = true).</summary>
    [HttpPost]
    [HasPermission(Permissions.Enrollments.Create)]
    public async Task<ActionResult<EnrollmentDto>> Create(CreateEnrollmentRequest request, CancellationToken ct)
    {
        var dto = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id, version = "1.0" }, dto);
    }

    /// <summary>Turns a pending hold into a confirmed enrollment.</summary>
    [HttpPost("{id:int}/confirm")]
    [HasPermission(Permissions.Enrollments.Confirm)]
    public async Task<ActionResult<EnrollmentDto>> Confirm(int id, CancellationToken ct) =>
        Ok(await service.ConfirmAsync(id, ct));

    /// <summary>Frees the seat. Money already paid stays; the open payment plan is cancelled.</summary>
    [HttpPost("{id:int}/cancel")]
    [HasPermission(Permissions.Enrollments.Cancel)]
    public async Task<ActionResult<EnrollmentDto>> Cancel(int id, CancellationToken ct) =>
        Ok(await service.CancelAsync(id, ct));

    /// <summary>Moves the student to another section of the same course.</summary>
    [HttpPost("{id:int}/transfer")]
    [HasPermission(Permissions.Enrollments.Transfer)]
    public async Task<ActionResult<EnrollmentDto>> Transfer(int id, TransferEnrollmentRequest request, CancellationToken ct) =>
        Ok(await service.TransferAsync(id, request, ct));
}
