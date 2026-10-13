using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Common.Models;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Exceptions;

namespace EducationalCenter.Application.Features.Branches;

public sealed class BranchService(IUnitOfWork uow, IBranchContext branchContext, IAuditLogger audit) : IBranchService
{
    /// <summary>The Super Admin sees every branch. Anyone else sees only their own (a user without a branch sees none).</summary>
    private int? VisibleBranchId => branchContext.IsSuperAdmin ? null : (branchContext.BranchId ?? -1);

    public async Task<BranchDto> GetByIdAsync(int id, CancellationToken ct = default) =>
        (await LoadAsync(id, ct)).ToDto();

    public async Task<PagedResult<BranchDto>> ListAsync(BranchListQuery query, CancellationToken ct = default)
    {
        var (items, total) = await uow.Branches.SearchAsync(
            query.Search?.Trim(), query.IsActive, VisibleBranchId, query.Page, query.PageSize, ct);
        return new PagedResult<BranchDto>(items.Select(b => b.ToDto()).ToList(), total, query.Page, query.PageSize);
    }

    public async Task<BranchDto> CreateAsync(CreateBranchRequest request, CancellationToken ct = default)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await uow.Branches.CodeExistsAsync(code, null, ct))
            throw new ConflictException($"A branch with the code '{code}' already exists.");

        var branch = new Branch
        {
            Code = code,
            Name = request.Name.Trim(),
            Governorate = request.Governorate?.Trim(),
            Address = request.Address?.Trim(),
            Phone = request.Phone?.Trim(),
            IsActive = true
        };

        return await uow.ExecuteInTransactionAsync(async () =>
        {
            await uow.Branches.AddAsync(branch, ct);
            await uow.SaveChangesAsync(ct);

            var dto = branch.ToDto();
            await audit.LogAsync("Branch.Create", nameof(Branch), branch.Id, null, dto, null, ct);
            await uow.SaveChangesAsync(ct);
            return dto;
        }, ct);
    }

    public async Task<BranchDto> UpdateAsync(int id, UpdateBranchRequest request, CancellationToken ct = default)
    {
        var branch = await LoadAsync(id, ct);
        var before = branch.ToDto();

        branch.Name = request.Name.Trim();
        branch.Governorate = request.Governorate?.Trim();
        branch.Address = request.Address?.Trim();
        branch.Phone = request.Phone?.Trim();

        var after = branch.ToDto();
        await audit.LogAsync("Branch.Update", nameof(Branch), branch.Id, before, after, null, ct);
        await uow.SaveChangesAsync(ct);
        return after;
    }

    public async Task<BranchDto> CloseAsync(int id, CancellationToken ct = default)
    {
        var branch = await LoadAsync(id, ct);
        if (!branch.IsActive)
            throw new ConflictException("This branch is already closed.");

        if (await uow.Branches.CountActiveAsync(ct) <= 1)
            throw new ConflictException("The last active branch cannot be closed.");

        branch.IsActive = false;
        await audit.LogAsync("Branch.Close", nameof(Branch), branch.Id, new { IsActive = true }, new { IsActive = false }, null, ct);
        await uow.SaveChangesAsync(ct);
        return branch.ToDto();
    }

    public async Task<BranchDto> ReopenAsync(int id, CancellationToken ct = default)
    {
        var branch = await LoadAsync(id, ct);
        if (branch.IsActive)
            throw new ConflictException("This branch is already active.");

        branch.IsActive = true;
        await audit.LogAsync("Branch.Reopen", nameof(Branch), branch.Id, new { IsActive = false }, new { IsActive = true }, null, ct);
        await uow.SaveChangesAsync(ct);
        return branch.ToDto();
    }

    private async Task<Branch> LoadAsync(int id, CancellationToken ct)
    {
        var branch = await uow.Branches.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Branch), id);

        // Someone else's branch looks exactly like a branch that does not exist.
        if (VisibleBranchId is { } only && only != id)
            throw new NotFoundException(nameof(Branch), id);

        return branch;
    }
}