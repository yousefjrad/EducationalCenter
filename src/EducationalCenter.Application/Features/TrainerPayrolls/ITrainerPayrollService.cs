using EducationalCenter.Application.Common.Models;

namespace EducationalCenter.Application.Features.TrainerPayrolls;

/// <remarks>
/// How the amount is calculated depends on the trainer's pay type:
/// Monthly = pay value x calendar months touched by the period;
/// Hourly = pay value x hours of the trainer's Held sessions in the period;
/// Percentage = pay value % of the SYP collected in the period from sections the trainer teaches.
/// </remarks>
public interface ITrainerPayrollService
{
    Task<PayrollDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PagedResult<PayrollDto>> ListAsync(PayrollListQuery query, CancellationToken ct = default);

    /// <summary>Preview only: shows the calculated amount and the figures behind it.</summary>
    Task<PayrollCalculationDto> CalculateAsync(PayrollRequest request, CancellationToken ct = default);

    /// <summary>Calculates and saves a Due payroll. Periods of the same trainer cannot overlap.</summary>
    Task<PayrollDto> CreateAsync(PayrollRequest request, CancellationToken ct = default);

    /// <summary>Marks a Due payroll as Paid (Admin only, API policy).</summary>
    Task<PayrollDto> PayAsync(int id, PayPayrollRequest request, CancellationToken ct = default);

    /// <summary>Cancels a payroll that is still Due. Paid payrolls cannot be cancelled.</summary>
    Task<PayrollDto> CancelAsync(int id, CancelPayrollRequest request, CancellationToken ct = default);
}
