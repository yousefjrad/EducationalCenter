using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Features.Expenses;

public static class ExpenseMappings
{
    public static ExpenseDto ToDto(this Expense e) => new(
        e.Id, e.Category, e.Description, e.Currency, e.Amount, e.ExchangeRate, e.AmountInSyp, e.ExpenseDate, e.RecordedByUserId);
}
