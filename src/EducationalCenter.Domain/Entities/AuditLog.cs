namespace EducationalCenter.Domain.Entities;

/// <summary>Append-only. Financial operations only.</summary>
public class AuditLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
}
