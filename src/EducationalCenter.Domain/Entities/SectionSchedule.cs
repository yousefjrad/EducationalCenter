namespace EducationalCenter.Domain.Entities;

/// <summary>Weekly recurring slot. Intentionally has no tracking fields.</summary>
public class SectionSchedule
{
    public int Id { get; set; }
    public int SectionId { get; set; }
    public Section Section { get; set; } = null!;
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}
