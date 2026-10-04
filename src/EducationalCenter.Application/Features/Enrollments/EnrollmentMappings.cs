using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Features.Enrollments;

public static class EnrollmentMappings
{
    /// <remarks>Student and Section (with Course) must be loaded.</remarks>
    public static EnrollmentDto ToDto(this Enrollment e) => new(
        e.Id,
        e.StudentId,
        e.Student.FullName,
        e.SectionId,
        e.Section.Name,
        e.Section.Course.Name,
        e.AgreedPrice,
        e.Status,
        e.EnrolledAt,
        e.HoldExpiresAt,
        e.TransferredFromEnrollmentId);
}
