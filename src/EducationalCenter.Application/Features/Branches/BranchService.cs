using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Exceptions;

namespace EducationalCenter.Application.Features.Branches;

public sealed class BranchService(IUnitOfWork uow, IBranchContext branchContext, IAuditLogger audit) : IBranchService
{
    public async Task<IReadOnlyList<BranchDto>> ListAsync(CancellationToken ct = default)
    {
        var all = await uow.Branches.ListAsync(ct);
        return all
            .Where(b => branchContext.IsSuperAdmin || b.Id == branchContext.BranchId)
            .OrderBy(b => b.Name)
            .Select(b => b.ToDto())
            .ToList();
    }

    public async Task<BranchDto> GetByIdAsync(int id, CancellationToken ct = default) =>
        (await FindAsync(id, ct)).ToDto();

    public async Task<BranchDto> CreateAsync(CreateBranchRequest request, CancellationToken ct = default)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await uow.Branches.CodeExistsAsync(code, null, ct))
            throw new ConflictException($"A branch with the code '{code}' already exists.");

        var branch = new Branch
        {
            Name = request.Name.Trim(),
            Code = code,
            Governorate = request.Governorate?.Trim(),
            Address = request.Address?.Trim(),
            Phone = request.Phone?.Trim(),
            IsActive = true
        };

        await uow.Branches.AddAsync(branch, ct);
        await uow.SaveChangesAsync(ct);

        await audit.LogAsync("Branch.Create", nameof(Branch), branch.Id, null, branch.ToDto(), null, ct);
        await uow.SaveChangesAsync(ct);
        return branch.ToDto();
    }

    public async Task<BranchDto> UpdateAsync(int id, UpdateBranchRequest request, CancellationToken ct = default)
    {
        var branch = await FindAsync(id, ct);
        var before = branch.ToDto();

        branch.Name = request.Name.Trim();
        branch.Governorate = request.Governorate?.Trim();
        branch.Address = request.Address?.Trim();
        branch.Phone = request.Phone?.Trim();

        await audit.LogAsync("Branch.Update", nameof(Branch), branch.Id, before, branch.ToDto(), null, ct);
        await uow.SaveChangesAsync(ct);
        return branch.ToDto();
    }

    public async Task<BranchDto> CloseAsync(int id, CancellationToken ct = default)
    {
        var branch = await FindAsync(id, ct);
        if (!branch.IsActive)
            throw new ConflictException("This branch is already closed.");

        if (await uow.Branches.CountActiveAsync(ct) <= 1)
            throw new ConflictException("The last active branch cannot be closed.");

        var before = branch.ToDto();
        branch.IsActive = false;

        await audit.LogAsync("Branch.Close", nameof(Branch), branch.Id, before, branch.ToDto(), null, ct);
        await uow.SaveChangesAsync(ct);
        return branch.ToDto();
    }

    public async Task<BranchDto> ReopenAsync(int id, CancellationToken ct = default)
    {
        var branch = await FindAsync(id, ct);
        if (branch.IsActive)
            throw new ConflictException("This branch is already active.");

        var before = branch.ToDto();
        branch.IsActive = true;

        await audit.LogAsync("Branch.Reopen", nameof(Branch), branch.Id, before, branch.ToDto(), null, ct);
        await uow.SaveChangesAsync(ct);
        return branch.ToDto();
    }

    /// <summary>A branch outside the caller's scope is reported as not found, so its existence is not revealed.</summary>
    private async Task<Branch> FindAsync(int id, CancellationToken ct)
    {
        var branch = await uow.Branches.GetByIdAsync(id, ct);
        if (branch is null || (!branchContext.IsSuperAdmin && branch.Id != branchContext.BranchId))
            throw new NotFoundException(nameof(Branch), id);
        return branch;
    }
}