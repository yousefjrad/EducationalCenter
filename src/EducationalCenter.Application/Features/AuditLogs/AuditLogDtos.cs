namespace EducationalCenter.Application.Features.AuditLogs;

/// <param name="OldValues">JSON snapshot before the change, if any.</param>
/// <param name="NewValues">JSON snapshot after the change, if any.</param>
public sealed record AuditLogDto(
    int Id,
    int UserId,
    string UserName,
    string Action,
    string EntityName,
    int EntityId,
    string? OldValues,
    string? NewValues,
    string? Reason,
    DateTime CreatedAt);

public sealed record AuditLogListQuery(
    int? UserId = null,
    string? EntityName = null,
    int? EntityId = null,
    string? Action = null,
    DateOnly? From = null,
    DateOnly? To = null,
    int Page = 1,
    int PageSize = 50);
