using EducationalCenter.Domain.Common;

namespace EducationalCenter.Domain.Entities;

public class Certificate : BaseEntity
{
    public int EnrollmentId { get; set; }
    public Enrollment Enrollment { get; set; } = null!;

    public string CertificateNumber { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public int IssuedByUserId { get; set; }
    public User IssuedByUser { get; set; } = null!;
    public bool IsOverride { get; set; }
    public string? OverrideReason { get; set; }
}
