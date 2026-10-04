namespace EducationalCenter.Application.Common.Interfaces;

/// <summary>
/// Produces the next unique sequential receipt number (e.g. "RCP-000123").
/// Implemented in Infrastructure; it must be safe when two receptionists save at the same time.
/// </summary>
public interface IReceiptNumberGenerator
{
    Task<string> NextAsync(CancellationToken ct = default);
}
