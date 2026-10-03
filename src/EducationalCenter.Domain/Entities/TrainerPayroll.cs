using EducationalCenter.Domain.Common;
using EducationalCenter.Domain.Enums;

namespace EducationalCenter.Domain.Entities;

public class TrainerPayroll : BaseEntity
{
    public int TrainerId { get; set; }
    public Trainer Trainer { get; set; } = null!;

    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public string CalculationMethod { get; set; } = string.Empty;
    public Currency Currency { get; set; } = Currency.Syp;
    public decimal Amount { get; set; }
    public decimal? ExchangeRate { get; set; }
    public decimal AmountInSyp { get; set; }
    public PayrollStatus Status { get; set; } = PayrollStatus.Due;
    public DateTime? PaidAt { get; set; }
    public int? PaidByUserId { get; set; }
    public User? PaidByUser { get; set; }
}
