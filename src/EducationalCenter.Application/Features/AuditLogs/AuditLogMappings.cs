using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Features.AuditLogs;

public static class AuditLogMappings
{
    /// <remarks>User must be loaded.</remarks>
    public static AuditLogDto ToDto(this AuditLog a) => new(
        a.Id, a.UserId, a.User.FullName, a.Action, a.EntityName, a.EntityId,
        a.OldValues, a.NewValues, a.Reason, a.CreatedAt);
}
