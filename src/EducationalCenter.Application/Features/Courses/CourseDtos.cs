namespace EducationalCenter.Application.Features.Courses;

public sealed record CourseDto(
    int Id,
    string Code,
    string Name,
    string? Description,
    string? Level,
    int DefaultDurationHours,
    decimal DefaultPrice,
    bool IsActive);

public sealed record CreateCourseRequest(
    string Code,
    string Name,
    string? Description,
    string? Level,
    int DefaultDurationHours,
    decimal DefaultPrice);

public sealed record UpdateCourseRequest(
    string Code,
    string Name,
    string? Description,
    string? Level,
    int DefaultDurationHours,
    decimal DefaultPrice,
    bool IsActive);

public sealed record CourseListQuery(
    string? Search = null,
    bool? IsActive = null,
    int Page = 1,
    int PageSize = 20);
