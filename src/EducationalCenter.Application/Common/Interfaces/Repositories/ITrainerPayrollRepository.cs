using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Enums;

namespace EducationalCenter.Application.Common.Interfaces.Repositories;

public interface ITrainerPayrollRepository : IRepository<TrainerPayroll>
{
    /// <summary>Tracked. Loads Trainer.</summary>
    Task<TrainerPayroll?> GetWithDetailsAsync(int id, CancellationToken ct = default);

    /// <summary>True if the trainer has a non-cancelled payroll overlapping the period.</summary>
    Task<bool> HasOverlapAsync(int trainerId, DateOnly start, DateOnly end, CancellationToken ct = default);

    /// <summary>
    /// Items come with Trainer loaded, newest period first. When <paramref name="from"/>/<paramref name="to"/>
    /// are given, payrolls whose period overlaps that range are returned.
    /// </summary>
    Task<(IReadOnlyList<TrainerPayroll> Items, int TotalCount)> SearchAsync(
        int? trainerId, PayrollStatus? status, DateOnly? from, DateOnly? to,
        int page, int pageSize, CancellationToken ct = default);
}
