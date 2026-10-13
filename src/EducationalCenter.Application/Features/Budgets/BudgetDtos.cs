using EducationalCenter.Domain.Enums;

namespace EducationalCenter.Application.Features.Budgets;

/// <param name="SpentSyp">Expenses of the branch in the budget month, in SYP.</param>
/// <param name="RemainingSyp">Approved amount minus spent (negative when over budget). Null until approved.</param>
public sealed record BudgetDto(
    int Id,
    int BranchId,
    string BranchName,
    DateOnly Month,
    decimal RequestedAmountSyp,
    decimal? ApprovedAmountSyp,
    BudgetStatus Status,
    string? Notes,
    int RequestedByUserId,
    string RequestedByName,
    DateTime? DecidedAt,
    decimal SpentSyp,
    decimal? RemainingSyp);

/// <param name="Month">Any date in the month; it is stored as the first day of that month.</param>
public sealed record CreateBudgetRequest(DateOnly Month, decimal RequestedAmountSyp, string? Notes);

public sealed record UpdateBudgetRequest(decimal RequestedAmountSyp, string? Notes);

/// <param name="ApprovedAmountSyp">Optional: defaults to the requested amount.</param>
public sealed record ApproveBudgetRequest(decimal? ApprovedAmountSyp);

public sealed record RejectBudgetRequest(string Reason);

public sealed record BudgetListQuery(
    BudgetStatus? Status = null,
    DateOnly? From = null,
    DateOnly? To = null,
    int? BranchId = null,
    int Page = 1,
    int PageSize = 20);

/// <summary>A branch whose expenses in the month passed its approved budget.</summary>
public sealed record BudgetOverrunRow(int BranchId, string BranchName, decimal ApprovedAmountSyp, decimal SpentSyp, decimal OverBySyp);