namespace EducationalCenter.Domain.Entities;

/// <summary>Seeded from code (see Constants/Permissions.cs), never created by users.</summary>
public class Permission
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
