using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Common.Interfaces.Repositories;

public interface ITrainerRepository : IRepository<Trainer>
{
    /// <summary>True if the trainer is referenced by any section, class session or payroll record.</summary>
    Task<bool> IsInUseAsync(int trainerId, CancellationToken ct = default);

    Task<(IReadOnlyList<Trainer> Items, int TotalCount)> SearchAsync(
        string? search, bool? isActive, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Active trainers whose full name matches (case-insensitive). More than one means the name is ambiguous.</summary>
    Task<IReadOnlyList<Trainer>> FindActiveByNameAsync(string fullName, CancellationToken ct = default);
}
