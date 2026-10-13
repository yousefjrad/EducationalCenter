using EducationalCenter.Application.Common.Exceptions;
using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Common.Models;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Enums;
using EducationalCenter.Domain.Exceptions;

namespace EducationalCenter.Application.Features.Budgets;

public sealed class BudgetService(
    IUnitOfWork uow,
    IClock clock,
    ICurrentUser currentUser,
    IBranchContext branchContext,
    IAuditLogger audit) : IBudgetService
{
    private const int MaxNotes = 500;

    public async Task<BudgetDto> GetByIdAsync(int id, CancellationToken ct = default) =>
        await ToDtoAsync(await FindAsync(id, ct), ct);

    public async Task<PagedResult<BudgetDto>> ListAsync(BudgetListQuery query, CancellationToken ct = default)
    {
        var (items, total) = await uow.Budgets.SearchAsync(
            query.Status, query.From, query.To, query.BranchId, query.Page, query.PageSize, ct);

        var dtos = new List<BudgetDto>();
        foreach (var budget in items)
            dtos.Add(await ToDtoAsync(budget, ct));

        return new PagedResult<BudgetDto>(dtos, total, query.Page, query.PageSize);
    }

    public async Task<BudgetDto> CreateAsync(CreateBudgetRequest request, CancellationToken ct = default)
    {
        var userId = currentUser.RequireUserId();

        if (branchContext.BranchId is not int branchId || branchId <= 0)
            throw new RequestValidationException("branch", "Select a branch first: a budget belongs to one branch (send the X-Branch-Id header).");

        var month = FirstOfMonth(request.Month);
        var currentMonth = FirstOfMonth(DateOnly.FromDateTime(clock.UtcNow));
        if (month < currentMonth)
            throw new RequestValidationException("month", "A budget cannot be requested for a past month.");

        if (await uow.Budgets.ExistsAsync(month, branchId, null, ct))
            throw new ConflictException("This branch already has a budget for that month.");

        var budget = new Budget
        {
            Month = month,
            RequestedAmountSyp = request.RequestedAmountSyp,
            Status = BudgetStatus.Draft,
            Notes = Clean(request.Notes),
            RequestedByUserId = userId
        };

        await uow.Budgets.AddAsync(budget, ct);
        await uow.SaveChangesAsync(ct);

        await audit.LogAsync("Budget.Created", nameof(Budget), budget.Id, null, Snapshot(budget), null, ct);
        await uow.SaveChangesAsync(ct);

        return await ToDtoAsync(await FindAsync(budget.Id, ct), ct);
    }

    public async Task<BudgetDto> UpdateAsync(int id, UpdateBudgetRequest request, CancellationToken ct = default)
    {
        var budget = await FindAsync(id, ct);
        if (budget.Status is not (BudgetStatus.Draft or BudgetStatus.Rejected))
            throw new ConflictException("Only a draft or a rejected budget can be edited.");

        var before = Snapshot(budget);

        budget.RequestedAmountSyp = request.RequestedAmountSyp;
        budget.Notes = Clean(request.Notes);
        budget.Status = BudgetStatus.Draft;
        budget.DecidedByUserId = null;
        budget.DecidedAt = null;

        await audit.LogAsync("Budget.Updated", nameof(Budget), budget.Id, before, Snapshot(budget), null, ct);
        await uow.SaveChangesAsync(ct);
        return await ToDtoAsync(budget, ct);
    }

    public async Task<BudgetDto> SubmitAsync(int id, CancellationToken ct = default)
    {
        var budget = await FindAsync(id, ct);
        if (budget.Status is not (BudgetStatus.Draft or BudgetStatus.Rejected))
            throw new ConflictException("Only a draft or a rejected budget can be submitted.");

        var before = Snapshot(budget);
        budget.Status = BudgetStatus.Submitted;
        budget.DecidedByUserId = null;
        budget.DecidedAt = null;

        await audit.LogAsync("Budget.Submitted", nameof(Budget), budget.Id, before, Snapshot(budget), null, ct);
        await uow.SaveChangesAsync(ct);
        return await ToDtoAsync(budget, ct);
    }

    public async Task<BudgetDto> ApproveAsync(int id, ApproveBudgetRequest request, CancellationToken ct = default)
    {
        var userId = currentUser.RequireUserId();
        var budget = await FindAsync(id, ct);
        if (budget.Status != BudgetStatus.Submitted)
            throw new ConflictException("Only a submitted budget can be approved.");

        var before = Snapshot(budget);
        budget.Status = BudgetStatus.Approved;
        budget.ApprovedAmountSyp = request.ApprovedAmountSyp ?? budget.RequestedAmountSyp;
        budget.DecidedByUserId = userId;
        budget.DecidedAt = clock.UtcNow;

        await audit.LogAsync("Budget.Approved", nameof(Budget), budget.Id, before, Snapshot(budget), null, ct);
        await uow.SaveChangesAsync(ct);
        return await ToDtoAsync(budget, ct);
    }

    public async Task<BudgetDto> RejectAsync(int id, RejectBudgetRequest request, CancellationToken ct = default)
    {
        var userId = currentUser.RequireUserId();
        var budget = await FindAsync(id, ct);
        if (budget.Status != BudgetStatus.Submitted)
            throw new ConflictException("Only a submitted budget can be rejected.");

        var before = Snapshot(budget);
        var reason = request.Reason.Trim();

        budget.Status = BudgetStatus.Rejected;
        budget.DecidedByUserId = userId;
        budget.DecidedAt = clock.UtcNow;

        // The branch manager sees the reason next to the budget; the audit log keeps it too.
        var note = (string.IsNullOrWhiteSpace(budget.Notes) ? string.Empty : budget.Notes + " | ") + "Rejected: " + reason;
        budget.Notes = note.Length <= MaxNotes ? note : note[..MaxNotes];

        await audit.LogAsync("Budget.Rejected", nameof(Budget), budget.Id, before, Snapshot(budget), reason, ct);
        await uow.SaveChangesAsync(ct);
        return await ToDtoAsync(budget, ct);
    }

    private async Task<Budget> FindAsync(int id, CancellationToken ct) =>
        await uow.Budgets.GetWithDetailsAsync(id, ct) ?? throw new NotFoundException(nameof(Budget), id);

    private async Task<BudgetDto> ToDtoAsync(Budget b, CancellationToken ct)
    {
        var spent = await uow.Budgets.GetSpentSypAsync(b.BranchId, b.Month, ct);

        return new BudgetDto(
            b.Id,
            b.BranchId,
            b.Branch.Name,
            b.Month,
            b.RequestedAmountSyp,
            b.ApprovedAmountSyp,
            b.Status,
            b.Notes,
            b.RequestedByUserId,
            b.RequestedByUser.FullName,
            b.DecidedAt,
            spent,
            b.ApprovedAmountSyp is null ? null : b.ApprovedAmountSyp.Value - spent);
    }

    private static DateOnly FirstOfMonth(DateOnly date) => new(date.Year, date.Month, 1);

    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    private static object Snapshot(Budget b) => new
    {
        b.BranchId,
        b.Month,
        b.RequestedAmountSyp,
        b.ApprovedAmountSyp,
        b.Status,
        b.Notes
    };
}