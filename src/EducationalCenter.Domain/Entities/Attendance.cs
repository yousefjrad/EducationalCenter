using EducationalCenter.Domain.Common;

namespace EducationalCenter.Domain.Entities;

public class Attendance : BaseEntity
{
    public int ClassSessionId { get; set; }
    public ClassSession ClassSession { get; set; } = null!;
    public int EnrollmentId { get; set; }
    public Enrollment Enrollment { get; set; } = null!;

    public bool IsPresent { get; set; }
    public int RecordedByUserId { get; set; }
    public User RecordedByUser { get; set; } = null!;
}
