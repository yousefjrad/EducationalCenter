using EducationalCenter.Domain.Common;
using EducationalCenter.Domain.Enums;

namespace EducationalCenter.Domain.Entities;

public class Enrollment : BaseEntity
{
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public int SectionId { get; set; }
    public Section Section { get; set; } = null!;

    public decimal AgreedPrice { get; set; }
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Confirmed;
    public DateTime EnrolledAt { get; set; }
    public DateTime? HoldExpiresAt { get; set; }
    public int? TransferredFromEnrollmentId { get; set; }
    public Enrollment? TransferredFromEnrollment { get; set; }

    public ICollection<Attendance> Attendances { get; set; } = [];
    public PaymentPlan? PaymentPlan { get; set; }
    public Grade? Grade { get; set; }
    public Certificate? Certificate { get; set; }
}
