using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Features.TrainerPayrolls;

public static class PayrollMappings
{
    /// <remarks>Trainer must be loaded.</remarks>
    public static PayrollDto ToDto(this TrainerPayroll p) => new(
        p.Id,
        p.TrainerId,
        p.Trainer.FullName,
        p.PeriodStart,
        p.PeriodEnd,
        p.CalculationMethod,
        p.Currency,
        p.Amount,
        p.ExchangeRate,
        p.AmountInSyp,
        p.Status,
        p.PaidAt,
        p.PaidByUserId);
}
