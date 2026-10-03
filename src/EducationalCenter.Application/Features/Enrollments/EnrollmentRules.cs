using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Enums;
using EducationalCenter.Domain.Exceptions;

namespace EducationalCenter.Application.Features.Enrollments;

internal static class EnrollmentRules
{
    /// <summary>Students can enroll while a section is open for enrollment or already running.</summary>
    public static void EnsureSectionAccepts(Section section)
    {
        if (section.Status is not (SectionStatus.OpenForEnrollment or SectionStatus.InProgress))
            throw new ConflictException($"Enrollment is not available: the section is {section.Status}.");
    }
}
