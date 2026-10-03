using EducationalCenter.Domain.Common;

namespace EducationalCenter.Domain.Entities;

public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    /// <summary>System roles (Admin, Receptionist) cannot be deleted.</summary>
    public bool IsSystem { get; set; }

    public ICollection<User> Users { get; set; } = [];
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
