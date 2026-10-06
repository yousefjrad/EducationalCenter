using Asp.Versioning;
using EducationalCenter.API.Authorization;
using EducationalCenter.Application.Common.Models;
using EducationalCenter.Application.Features.TrainerPayrolls;
using EducationalCenter.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace EducationalCenter.API.Controllers;

[ApiVersion("1.0")]
public sealed class TrainerPayrollsController(ITrainerPayrollService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.TrainerPayroll.View)]
    public async Task<ActionResult<PagedResult<PayrollDto>>> List([FromQuery] PayrollListQuery query, CancellationToken ct) =>
        Ok(await service.ListAsync(query, ct));

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.TrainerPayroll.View)]
    public async Task<ActionResult<PayrollDto>> GetById(int id, CancellationToken ct) =>
        Ok(await service.GetByIdAsync(id, ct));

    /// <summary>Preview only: the calculated amount and the figures behind it. Nothing is saved.</summary>
    [HttpPost("calculate")]
    [HasPermission(Permissions.TrainerPayroll.Calculate)]
    public async Task<ActionResult<PayrollCalculationDto>> Calculate(PayrollRequest request, CancellationToken ct) =>
        Ok(await service.CalculateAsync(request, ct));

    /// <summary>Calculates and saves a Due payroll. Periods of the same trainer cannot overlap.</summary>
    [HttpPost]
    [HasPermission(Permissions.TrainerPayroll.Calculate)]
    public async Task<ActionResult<PayrollDto>> Create(PayrollRequest request, CancellationToken ct)
    {
        var dto = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id, version = "1.0" }, dto);
    }

    /// <summary>Marks a Due payroll as Paid. Send {} unless a new exchange rate applies at payment time.</summary>
    [HttpPost("{id:int}/pay")]
    [HasPermission(Permissions.TrainerPayroll.Pay)]
    public async Task<ActionResult<PayrollDto>> Pay(int id, PayPayrollRequest request, CancellationToken ct) =>
        Ok(await service.PayAsync(id, request, ct));

    /// <summary>Cancels a payroll that is still Due. Paid payrolls cannot be cancelled.</summary>
    [HttpPost("{id:int}/cancel")]
    [HasPermission(Permissions.TrainerPayroll.Pay)]
    public async Task<ActionResult<PayrollDto>> Cancel(int id, CancelPayrollRequest request, CancellationToken ct) =>
        Ok(await service.CancelAsync(id, request, ct));
}
