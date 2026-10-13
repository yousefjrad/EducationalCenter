using Asp.Versioning;
using EducationalCenter.API.Authorization;
using EducationalCenter.Application.Features.Branches;
using EducationalCenter.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace EducationalCenter.API.Controllers;

[ApiVersion("1.0")]
public sealed class BranchesController(IBranchService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.Branches.View)]
    public async Task<ActionResult<IReadOnlyList<BranchDto>>> List(CancellationToken ct) =>
        Ok(await service.ListAsync(ct));

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.Branches.View)]
    public async Task<ActionResult<BranchDto>> GetById(int id, CancellationToken ct) =>
        Ok(await service.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.Branches.Manage)]
    public async Task<ActionResult<BranchDto>> Create(CreateBranchRequest request, CancellationToken ct)
    {
        var dto = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id, version = "1.0" }, dto);
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.Branches.Manage)]
    public async Task<ActionResult<BranchDto>> Update(int id, UpdateBranchRequest request, CancellationToken ct) =>
        Ok(await service.UpdateAsync(id, request, ct));

    [HttpPost("{id:int}/close")]
    [HasPermission(Permissions.Branches.Manage)]
    public async Task<ActionResult<BranchDto>> Close(int id, CancellationToken ct) =>
        Ok(await service.CloseAsync(id, ct));

    [HttpPost("{id:int}/reopen")]
    [HasPermission(Permissions.Branches.Manage)]
    public async Task<ActionResult<BranchDto>> Reopen(int id, CancellationToken ct) =>
        Ok(await service.ReopenAsync(id, ct));
}