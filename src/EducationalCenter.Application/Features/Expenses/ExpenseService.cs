using EducationalCenter.Application.Common.Exceptions;
using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Common.Models;
using EducationalCenter.Application.Common.Money;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Exceptions;

namespace EducationalCenter.Application.Features.Expenses;

public sealed class ExpenseService(
    IUnitOfWork uow,
    IClock clock,
    ICurrentUser currentUser,
    IAuditLogger audit) : IExpenseService
{
    public async Task<ExpenseDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var expense = await uow.Expenses.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Expense), id);
        return expense.ToDto();
    }

    public async Task<PagedResult<ExpenseDto>> ListAsync(ExpenseListQuery query, CancellationToken ct = default)
    {
        var (items, total) = await uow.Expenses.SearchAsync(
            query.Category?.Trim(), query.From, query.To, query.Page, query.PageSize, ct);

        return new PagedResult<ExpenseDto>(items.Select(e => e.ToDto()).ToList(), total, query.Page, query.PageSize);
    }

    public Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken ct = default) =>
        uow.Expenses.GetCategoriesAsync(ct);

    public Task<ExpenseDto> CreateAsync(CreateExpenseRequest request, CancellationToken ct = default)
    {
        return uow.ExecuteInTransactionAsync(async () =>
        {
            var userId = currentUser.RequireUserId();
            EnsureNotInFuture(request.ExpenseDate);

            var expense = new Expense
            {
                Category = request.Category.Trim(),
                Description = request.Description.Trim(),
                Currency = request.Currency,
                Amount = request.Amount,
                ExchangeRate = request.ExchangeRate,
                AmountInSyp = CurrencyConversion.ToSyp(request.Currency, request.Amount, request.ExchangeRate),
                ExpenseDate = request.ExpenseDate,
                RecordedByUserId = userId
            };

            await uow.Expenses.AddAsync(expense, ct);
            await uow.SaveChangesAsync(ct);

            await audit.LogAsync(
                "Expense.Created", nameof(Expense), expense.Id,
                oldValues: null,
                newValues: Snapshot(expense),
                reason: null, ct);
            await uow.SaveChangesAsync(ct);

            return expense.ToDto();
        }, ct);
    }

    public Task<ExpenseDto> UpdateAsync(int id, UpdateExpenseRequest request, CancellationToken ct = default)
    {
        return uow.ExecuteInTransactionAsync(async () =>
        {
            var expense = await uow.Expenses.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Expense), id);
            EnsureNotInFuture(request.ExpenseDate);

            var before = Snapshot(expense);

            expense.Category = request.Category.Trim();
            expense.Description = request.Description.Trim();
            expense.Currency = request.Currency;
            expense.Amount = request.Amount;
            expense.ExchangeRate = request.ExchangeRate;
            expense.AmountInSyp = CurrencyConversion.ToSyp(request.Currency, request.Amount, request.ExchangeRate);
            expense.ExpenseDate = request.ExpenseDate;

            await uow.SaveChangesAsync(ct);

            await audit.LogAsync(
                "Expense.Updated", nameof(Expense), expense.Id,
                oldValues: before,
                newValues: Snapshot(expense),
                reason: null, ct);
            await uow.SaveChangesAsync(ct);

            return expense.ToDto();
        }, ct);
    }

    /// <summary>One day of tolerance so local time zones ahead of UTC are not blocked.</summary>
    private void EnsureNotInFuture(DateOnly date)
    {
        var latestAllowed = DateOnly.FromDateTime(clock.UtcNow).AddDays(1);
        if (date > latestAllowed)
            throw new RequestValidationException("expenseDate", "'expenseDate' cannot be in the future.");
    }

    private static object Snapshot(Expense e) => new
    {
        e.Category,
        e.Description,
        e.Currency,
        e.Amount,
        e.ExchangeRate,
        e.AmountInSyp,
        e.ExpenseDate
    };
}
