using EducationalCenter.Domain.Common;

namespace EducationalCenter.Domain.Entities;

public class Branch : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Short unique code used as the prefix of receipt and certificate numbers, for example "DM".</summary>
    public string Code { get; set; } = string.Empty;

    public string? Governorate { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }

    /// <summary>A closed branch keeps its data for reading and reports, but accepts no new records.</summary>
    public bool IsActive { get; set; } = true;
}