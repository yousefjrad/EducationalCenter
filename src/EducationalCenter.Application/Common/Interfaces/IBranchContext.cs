namespace EducationalCenter.Application.Common.Interfaces;

/// <summary>
/// The branch scope of the current request (implemented in the API layer).
/// BranchId is the one branch the request works in; null means no scope (Super Admin viewing all branches, or a background job).
/// A signed-in user without a branch gets -1, which matches nothing.
/// </summary>
public interface IBranchContext
{
    int? BranchId { get; }
    bool IsSuperAdmin { get; }
}