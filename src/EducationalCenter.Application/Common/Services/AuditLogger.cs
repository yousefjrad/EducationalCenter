using System.Text.Json;
using System.Text.Json.Serialization;
using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Common.Services;

public sealed class AuditLogger(IUnitOfWork uow, IClock clock, ICurrentUser currentUser) : IAuditLogger
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public Task LogAsync(
        string action, string entityName, int entityId,
        object? oldValues, object? newValues, string? reason,
        CancellationToken ct = default)
    {
        var entry = new AuditLog
        {
            UserId = currentUser.RequireUserId(),
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            OldValues = oldValues is null ? null : JsonSerializer.Serialize(oldValues, JsonOptions),
            NewValues = newValues is null ? null : JsonSerializer.Serialize(newValues, JsonOptions),
            Reason = reason?.Trim(),
            CreatedAt = clock.UtcNow
        };

        return uow.AuditLogs.AddAsync(entry, ct);
    }
}
