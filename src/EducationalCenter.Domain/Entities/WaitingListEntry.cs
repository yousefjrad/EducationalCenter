using EducationalCenter.Domain.Common;
using EducationalCenter.Domain.Enums;

namespace EducationalCenter.Domain.Entities;

public class WaitingListEntry : BaseEntity
{
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public int SectionId { get; set; }
    public Section Section { get; set; } = null!;

    public int Position { get; set; }
    public WaitingListStatus Status { get; set; } = WaitingListStatus.Waiting;
    public DateTime AddedAt { get; set; }
    public int? PromotedEnrollmentId { get; set; }
    public Enrollment? PromotedEnrollment { get; set; }
}
