namespace EducationalCenter.Application.Common.Interfaces;

/// <summary>
/// Appends an AuditLog row for a financial operation. It only adds the row to the unit of work;
/// the caller saves, so the log is committed in the same transaction as the operation.
/// </summary>
public interface IAuditLogger
{
    Task LogAsync(
        string action, string entityName, int entityId,
        object? oldValues, object? newValues, string? reason,
        CancellationToken ct = default);
}
