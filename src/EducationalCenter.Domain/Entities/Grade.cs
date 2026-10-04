using EducationalCenter.Domain.Common;

namespace EducationalCenter.Domain.Entities;

public class Grade : BaseEntity
{
    public int EnrollmentId { get; set; }
    public Enrollment Enrollment { get; set; } = null!;

    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public bool IsPassed { get; set; }
    public int RecordedByUserId { get; set; }
    public User RecordedByUser { get; set; } = null!;
}
