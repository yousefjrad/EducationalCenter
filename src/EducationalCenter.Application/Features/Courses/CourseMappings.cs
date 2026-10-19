using EducationalCenter.Application.Common.Services;
using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Features.Courses;

public static class CourseMappings
{
    public static CourseDto ToDto(this Course c, IReadOnlyList<int>? branchIds = null) => new(
        c.Id, c.Code, c.Name, c.Description, c.Level, c.DefaultDurationHours, c.DefaultPrice, c.IsActive, branchIds, RowVersionCodec.Encode(c.RowVersion));
}
