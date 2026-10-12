using EducationalCenter.Domain.Common;
using EducationalCenter.Domain.Enums;

namespace EducationalCenter.Domain.Entities;

/// <summary>One monthly budget per branch: requested by the branch manager, decided by the Super Admin.</summary>
public class Budget : BaseEntity, IBranchOwned
{
    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    /// <summary>First day of the month the budget covers.</summary>
    public DateOnly Month { get; set; }

    public decimal RequestedAmountSyp { get; set; }
    public decimal? ApprovedAmountSyp { get; set; }
    public BudgetStatus Status { get; set; } = BudgetStatus.Draft;
    public string? Notes { get; set; }

    public int RequestedByUserId { get; set; }
    public User RequestedByUser { get; set; } = null!;
    public int? DecidedByUserId { get; set; }
    public User? DecidedByUser { get; set; }
    public DateTime? DecidedAt { get; set; }
}