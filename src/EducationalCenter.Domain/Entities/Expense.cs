using EducationalCenter.Domain.Common;
using EducationalCenter.Domain.Enums;

namespace EducationalCenter.Domain.Entities;

public class Expense : BaseEntity
{
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Currency Currency { get; set; } = Currency.Syp;
    public decimal Amount { get; set; }
    public decimal? ExchangeRate { get; set; }
    public decimal AmountInSyp { get; set; }
    public DateOnly ExpenseDate { get; set; }
    public int RecordedByUserId { get; set; }
    public User RecordedByUser { get; set; } = null!;
}
