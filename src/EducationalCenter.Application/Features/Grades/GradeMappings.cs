using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Features.Grades;

public static class GradeMappings
{
    /// <remarks>The enrollment must have Student and Section loaded.</remarks>
    public static GradeDto ToDto(this Grade g, Enrollment e) => new(
        g.Id,
        e.Id,
        e.StudentId,
        e.Student.FullName,
        e.Section.Name,
        g.Score,
        g.MaxScore,
        g.MaxScore == 0 ? 0m : Math.Round(g.Score * 100m / g.MaxScore, 2),
        g.IsPassed);
}
