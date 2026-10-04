using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Features.Certificates;

public static class CertificateMappings
{
    /// <remarks>The enrollment must have Student and Section (with Course) loaded.</remarks>
    public static CertificateDto ToDto(this Certificate c, Enrollment e) => new(
        c.Id,
        e.Id,
        e.StudentId,
        e.Student.FullName,
        e.Section.Course.Name,
        e.Section.Name,
        c.CertificateNumber,
        c.IssuedAt,
        c.IsOverride,
        c.OverrideReason);
}
