using EducationalCenter.Domain.Common;
using EducationalCenter.Domain.Enums;

namespace EducationalCenter.Domain.Entities;

public class ClassSession : BaseEntity
{
    public int SectionId { get; set; }
    public Section Section { get; set; } = null!;
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;
    public int TrainerId { get; set; }
    public Trainer Trainer { get; set; } = null!;
    public ClassSessionStatus Status { get; set; } = ClassSessionStatus.Scheduled;
    public string? Notes { get; set; }

    public ICollection<Attendance> Attendances { get; set; } = [];
}
