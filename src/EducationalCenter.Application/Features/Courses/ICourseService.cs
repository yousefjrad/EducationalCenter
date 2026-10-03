using EducationalCenter.Application.Common.Models;

namespace EducationalCenter.Application.Features.Courses;

public interface ICourseService
{
    Task<CourseDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PagedResult<CourseDto>> ListAsync(CourseListQuery query, CancellationToken ct = default);
    Task<CourseDto> CreateAsync(CreateCourseRequest request, CancellationToken ct = default);
    Task<CourseDto> UpdateAsync(int id, UpdateCourseRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
