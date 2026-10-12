namespace EducationalCenter.Domain.Common;

/// <summary>An operational record that belongs to exactly one branch.</summary>
public interface IBranchOwned
{
    int BranchId { get; set; }
}