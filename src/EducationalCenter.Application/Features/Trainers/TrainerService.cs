using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Common.Models;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Exceptions;

namespace EducationalCenter.Application.Features.Trainers;

public sealed class TrainerService(IUnitOfWork uow, IBranchContext branchContext, IAuditLogger audit) : ITrainerService
{
    public async Task<TrainerDto> GetByIdAsync(int id, CancellationToken ct = default) =>
        (await FindAsync(id, ct)).ToDto();

    public async Task<PagedResult<TrainerDto>> ListAsync(TrainerListQuery query, CancellationToken ct = default)
    {
        var (items, total) = branchContext.BranchId is int branchId
            ? await uow.Trainers.SearchInBranchAsync(query.Search?.Trim(), query.IsActive, branchId, query.Page, query.PageSize, ct)
            : await uow.Trainers.SearchAsync(query.Search?.Trim(), query.IsActive, query.Page, query.PageSize, ct);

        var branchIds = branchContext.IsSuperAdmin
            ? await uow.BranchLinks.GetTrainerBranchIdsAsync(items.Select(t => t.Id).ToList(), ct)
            : new Dictionary<int, IReadOnlyList<int>>();

        return new PagedResult<TrainerDto>(
            items.Select(t => t.ToDto(branchIds.TryGetValue(t.Id, out var b) ? b : null)).ToList(),
            total, query.Page, query.PageSize);
    }

    /// <remarks>A trainer created inside a branch is linked to it automatically (see AppDbContext).</remarks>
    public async Task<TrainerDto> CreateAsync(CreateTrainerRequest request, CancellationToken ct = default)
    {
        var trainer = new Trainer
        {
            FullName = request.FullName.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            Specialty = request.Specialty?.Trim(),
            PayType = request.PayType,
            PayValue = request.PayValue,
            PayCurrency = request.PayCurrency,
            IsActive = true
        };

        await uow.Trainers.AddAsync(trainer, ct);
        await uow.SaveChangesAsync(ct);

        await audit.LogAsync("Trainer.Create", nameof(Trainer), trainer.Id, null, Snapshot(trainer), null, ct);
        await uow.SaveChangesAsync(ct);
        return trainer.ToDto();
    }

    public async Task<TrainerDto> UpdateAsync(int id, UpdateTrainerRequest request, CancellationToken ct = default)
    {
        var trainer = await FindAsync(id, ct);
        var before = Snapshot(trainer);

        trainer.FullName = request.FullName.Trim();
        trainer.PhoneNumber = request.PhoneNumber.Trim();
        trainer.Specialty = request.Specialty?.Trim();
        trainer.PayType = request.PayType;
        trainer.PayValue = request.PayValue;
        trainer.PayCurrency = request.PayCurrency;
        trainer.IsActive = request.IsActive;

        await audit.LogAsync("Trainer.Update", nameof(Trainer), trainer.Id, before, Snapshot(trainer), null, ct);

        uow.Trainers.Update(trainer);
        await uow.SaveChangesAsync(ct);
        return trainer.ToDto();
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var trainer = await FindAsync(id, ct);

        if (await uow.Trainers.IsInUseAsync(id, ct))
            throw new ConflictException("This trainer has sections, sessions or payroll records and cannot be deleted. Deactivate them instead.");

        if (await uow.BranchLinks.IsTrainerUsedByOtherBranchesAsync(id, branchContext.BranchId, ct))
            throw new ConflictException("This trainer also teaches at other branches and cannot be deleted. Deactivate them instead.");

        await audit.LogAsync("Trainer.Delete", nameof(Trainer), trainer.Id, new { trainer.FullName, trainer.PhoneNumber, trainer.Specialty }, null, null, ct);

        uow.Trainers.Remove(trainer);
        await uow.SaveChangesAsync(ct);
    }

    private static object Snapshot(Trainer t) =>
        new { t.FullName, t.PhoneNumber, t.Specialty, t.PayType, t.PayValue, t.PayCurrency, t.IsActive };

    /// <summary>Inside a branch, a trainer of another branch is reported as not found.</summary>
    private async Task<Trainer> FindAsync(int id, CancellationToken ct)
    {
        var trainer = await uow.Trainers.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Trainer), id);

        if (branchContext.BranchId is int branchId && !await uow.BranchLinks.IsTrainerInBranchAsync(id, branchId, ct))
            throw new NotFoundException(nameof(Trainer), id);

        return trainer;
    }
}