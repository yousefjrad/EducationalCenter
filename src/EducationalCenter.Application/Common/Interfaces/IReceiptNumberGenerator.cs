namespace EducationalCenter.Application.Common.Interfaces;

/// <summary>
/// Produces the next unique sequential receipt number of a branch (e.g. "MN-RCP-00123": branch code, then a sequence of that branch).
/// Implemented in Infrastructure; it must be safe when two receptionists save at the same time.
/// </summary>
public interface IReceiptNumberGenerator
{
    Task<string> NextAsync(int branchId, CancellationToken ct = default);
}