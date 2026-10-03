using EducationalCenter.Domain.Common;
using EducationalCenter.Domain.Enums;

namespace EducationalCenter.Domain.Entities;

public class Section : BaseEntity
{
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public int TrainerId { get; set; }
    public Trainer Trainer { get; set; } = null!;
    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal Price { get; set; }
    public int Capacity { get; set; }
    public int MinStudents { get; set; }
    public SectionStatus Status { get; set; } = SectionStatus.Draft;

    public ICollection<SectionSchedule> Schedules { get; set; } = [];
    public ICollection<ClassSession> Sessions { get; set; } = [];
    public ICollection<Enrollment> Enrollments { get; set; } = [];
    public ICollection<WaitingListEntry> WaitingList { get; set; } = [];
}
