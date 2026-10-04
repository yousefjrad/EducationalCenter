using EducationalCenter.Application.Features.Reports;
using EducationalCenter.Domain.Enums;

namespace EducationalCenter.Application.Common.Interfaces.Repositories;

/// <summary>
/// Read-only aggregate queries for reports. They should be projections computed in the database,
/// never full entity graphs loaded into memory.
/// </summary>
public interface IReportRepository
{
    /// <summary>
    /// Valid payments grouped by year and month of PaidAt (UTC), between the two dates inclusive,
    /// oldest month first. Months without payments may be omitted.
    /// </summary>
    Task<IReadOnlyList<MonthlyRevenueRow>> GetMonthlyRevenueAsync(DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>
    /// Confirmed or Completed enrollments whose price is not fully paid (price minus valid payments > 0),
    /// largest remaining first. Optionally limited to one section or course.
    /// </summary>
    Task<IReadOnlyList<StudentBalanceRow>> GetStudentBalancesAsync(int? sectionId, int? courseId, CancellationToken ct = default);

    /// <summary>
    /// Installments of Open plans with DueDate before <paramref name="asOf"/> that are not fully paid,
    /// oldest due date first. DaysOverdue is left at 0 (the service computes it).
    /// </summary>
    Task<IReadOnlyList<OverdueInstallmentRow>> GetOverdueInstallmentsAsync(DateOnly asOf, int? sectionId, CancellationToken ct = default);

    /// <summary>
    /// Between the two dates inclusive: valid payments by UTC date of PaidAt, expenses by ExpenseDate,
    /// and payrolls with status Paid by UTC date of PaidAt (all in SYP), plus expenses per category.
    /// </summary>
    Task<NetProfitFigures> GetNetProfitFiguresAsync(DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>
    /// One row per section (Course, Trainer and Room names included). SeatsTaken follows the same rule as
    /// IEnrollmentRepository.CountSeatsTakenAsync; WaitingCount counts Waiting entries.
    /// When <paramref name="status"/> is null only Draft, OpenForEnrollment and InProgress sections are returned.
    /// </summary>
    Task<IReadOnlyList<SectionOccupancyRow>> GetSectionOccupancyAsync(
        DateTime utcNow, SectionStatus? status, int? courseId, CancellationToken ct = default);
}
