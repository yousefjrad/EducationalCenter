using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Common.Models;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Exceptions;

namespace EducationalCenter.Application.Features.Trainers;

public sealed class TrainerService(IUnitOfWork uow) : ITrainerService
{
    public async Task<TrainerDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var trainer = await uow.Trainers.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Trainer), id);
        return trainer.ToDto();
    }

    public async Task<PagedResult<TrainerDto>> ListAsync(TrainerListQuery query, CancellationToken ct = default)
    {
        var (items, total) = await uow.Trainers.SearchAsync(
            query.Search?.Trim(), query.IsActive, query.Page, query.PageSize, ct);
        return new PagedResult<TrainerDto>(items.Select(t => t.ToDto()).ToList(), total, query.Page, query.PageSize);
    }

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
        return trainer.ToDto();
    }

    public async Task<TrainerDto> UpdateAsync(int id, UpdateTrainerRequest request, CancellationToken ct = default)
    {
        var trainer = await uow.Trainers.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Trainer), id);

        trainer.FullName = request.FullName.Trim();
        trainer.PhoneNumber = request.PhoneNumber.Trim();
        trainer.Specialty = request.Specialty?.Trim();
        trainer.PayType = request.PayType;
        trainer.PayValue = request.PayValue;
        trainer.PayCurrency = request.PayCurrency;
        trainer.IsActive = request.IsActive;

        uow.Trainers.Update(trainer);
        await uow.SaveChangesAsync(ct);
        return trainer.ToDto();
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var trainer = await uow.Trainers.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Trainer), id);

        if (await uow.Trainers.IsInUseAsync(id, ct))
            throw new ConflictException("This trainer has sections, sessions or payroll records and cannot be deleted. Deactivate them instead.");

        uow.Trainers.Remove(trainer);
        await uow.SaveChangesAsync(ct);
    }
}
