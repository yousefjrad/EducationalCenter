using Asp.Versioning;
using EducationalCenter.API.Authorization;
using EducationalCenter.Application.Common.Models;
using EducationalCenter.Application.Features.Expenses;
using EducationalCenter.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace EducationalCenter.API.Controllers;

[ApiVersion("1.0")]
public sealed class ExpensesController(IExpenseService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.Expenses.View)]
    public async Task<ActionResult<PagedResult<ExpenseDto>>> List([FromQuery] ExpenseListQuery query, CancellationToken ct) =>
        Ok(await service.ListAsync(query, ct));

    /// <summary>Categories used so far, offered as suggestions when entering an expense.</summary>
    [HttpGet("categories")]
    [HasPermission(Permissions.Expenses.View)]
    public async Task<ActionResult<IReadOnlyList<string>>> GetCategories(CancellationToken ct) =>
        Ok(await service.GetCategoriesAsync(ct));

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.Expenses.View)]
    public async Task<ActionResult<ExpenseDto>> GetById(int id, CancellationToken ct) =>
        Ok(await service.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.Expenses.Create)]
    public async Task<ActionResult<ExpenseDto>> Create(CreateExpenseRequest request, CancellationToken ct)
    {
        var dto = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id, version = "1.0" }, dto);
    }

    /// <summary>Expenses can be corrected (audited) but never deleted, so there is no DELETE.</summary>
    [HttpPut("{id:int}")]
    [HasPermission(Permissions.Expenses.Update)]
    public async Task<ActionResult<ExpenseDto>> Update(int id, UpdateExpenseRequest request, CancellationToken ct) =>
        Ok(await service.UpdateAsync(id, request, ct));
}
