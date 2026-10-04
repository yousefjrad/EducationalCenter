using EducationalCenter.Domain.Enums;

namespace EducationalCenter.Application.Features.Expenses;

public sealed record ExpenseDto(
    int Id,
    string Category,
    string Description,
    Currency Currency,
    decimal Amount,
    decimal? ExchangeRate,
    decimal AmountInSyp,
    DateOnly ExpenseDate,
    int RecordedByUserId);

/// <param name="Category">Free text (rent, electricity, supplies...). Existing categories are offered as suggestions.</param>
/// <param name="ExchangeRate">SYP per 1 USD. Required for USD, must be empty for SYP.</param>
public sealed record CreateExpenseRequest(
    string Category,
    string Description,
    Currency Currency,
    decimal Amount,
    decimal? ExchangeRate,
    DateOnly ExpenseDate);

public sealed record UpdateExpenseRequest(
    string Category,
    string Description,
    Currency Currency,
    decimal Amount,
    decimal? ExchangeRate,
    DateOnly ExpenseDate);

public sealed record ExpenseListQuery(
    string? Category = null,
    DateOnly? From = null,
    DateOnly? To = null,
    int Page = 1,
    int PageSize = 20);
