using Asp.Versioning;
using EducationalCenter.API.Authorization;
using EducationalCenter.Application.Common.Models;
using EducationalCenter.Application.Features.Enrollments;
using EducationalCenter.Application.Features.WaitingList;
using EducationalCenter.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace EducationalCenter.API.Controllers;

[ApiVersion("1.0")]
public sealed class WaitingListController(IWaitingListService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.WaitingList.View)]
    public async Task<ActionResult<PagedResult<WaitingListEntryDto>>> List([FromQuery] WaitingListQuery query, CancellationToken ct) =>
        Ok(await service.ListAsync(query, ct));

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.WaitingList.View)]
    public async Task<ActionResult<WaitingListEntryDto>> GetById(int id, CancellationToken ct) =>
        Ok(await service.GetByIdAsync(id, ct));

    /// <summary>Only allowed when the section is full.</summary>
    [HttpPost]
    [HasPermission(Permissions.WaitingList.Manage)]
    public async Task<ActionResult<WaitingListEntryDto>> Add(AddToWaitingListRequest request, CancellationToken ct)
    {
        var dto = await service.AddAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id, version = "1.0" }, dto);
    }

    [HttpPost("{id:int}/cancel")]
    [HasPermission(Permissions.WaitingList.Manage)]
    public async Task<ActionResult<WaitingListEntryDto>> Cancel(int id, CancellationToken ct) =>
        Ok(await service.CancelAsync(id, ct));

    /// <summary>Manual promotion: creates the enrollment and closes the entry. Send {} for a confirmed enrollment.</summary>
    [HttpPost("{id:int}/promote")]
    [HasPermission(Permissions.WaitingList.Manage)]
    public async Task<ActionResult<EnrollmentDto>> Promote(int id, PromoteWaitingListEntryRequest request, CancellationToken ct) =>
        Ok(await service.PromoteAsync(id, request, ct));
}
