using EducationalCenter.Domain.Common;

namespace EducationalCenter.Domain.Entities;

public class Course : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Level { get; set; }
    public int DefaultDurationHours { get; set; }
    public decimal DefaultPrice { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Section> Sections { get; set; } = [];
}
