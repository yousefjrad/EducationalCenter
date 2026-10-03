using EducationalCenter.Domain.Common;

namespace EducationalCenter.Domain.Entities;

public class Room : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Section> Sections { get; set; } = [];
    public ICollection<ClassSession> ClassSessions { get; set; } = [];
}
