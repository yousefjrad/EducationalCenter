using Asp.Versioning;
using EducationalCenter.API.Authorization;
using EducationalCenter.Application.Common.Models;
using EducationalCenter.Application.Features.Budgets;
using EducationalCenter.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace EducationalCenter.API.Controllers;

[ApiVersion("1.0")]
public sealed class BudgetsController(IBudgetService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.Budgets.View)]
    public async Task<ActionResult<PagedResult<BudgetDto>>> List([FromQuery] BudgetListQuery query, CancellationToken ct) =>
        Ok(await service.ListAsync(query, ct));

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.Budgets.View)]
    public async Task<ActionResult<BudgetDto>> GetById(int id, CancellationToken ct) =>
        Ok(await service.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.Budgets.Request)]
    public async Task<ActionResult<BudgetDto>> Create(CreateBudgetRequest request, CancellationToken ct)
    {
        var dto = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id, version = "1.0" }, dto);
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.Budgets.Request)]
    public async Task<ActionResult<BudgetDto>> Update(int id, UpdateBudgetRequest request, CancellationToken ct) =>
        Ok(await service.UpdateAsync(id, request, ct));

    [HttpPost("{id:int}/submit")]
    [HasPermission(Permissions.Budgets.Request)]
    public async Task<ActionResult<BudgetDto>> Submit(int id, CancellationToken ct) =>
        Ok(await service.SubmitAsync(id, ct));

    [HttpPost("{id:int}/approve")]
    [HasPermission(Permissions.Budgets.Approve)]
    public async Task<ActionResult<BudgetDto>> Approve(int id, ApproveBudgetRequest request, CancellationToken ct) =>
        Ok(await service.ApproveAsync(id, request, ct));

    [HttpPost("{id:int}/reject")]
    [HasPermission(Permissions.Budgets.Approve)]
    public async Task<ActionResult<BudgetDto>> Reject(int id, RejectBudgetRequest request, CancellationToken ct) =>
        Ok(await service.RejectAsync(id, request, ct));
}