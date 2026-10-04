using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Features.Courses;

public static class CourseMappings
{
    public static CourseDto ToDto(this Course c) => new(
        c.Id, c.Code, c.Name, c.Description, c.Level, c.DefaultDurationHours, c.DefaultPrice, c.IsActive);
}
