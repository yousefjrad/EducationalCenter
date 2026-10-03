using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Common.Models;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Exceptions;

namespace EducationalCenter.Application.Features.Courses;

/// <remarks>
/// Request-shape validation (FluentValidation) happens before this service, in an API filter.
/// Rules that need the database live here.
/// </remarks>
public sealed class CourseService(IUnitOfWork uow) : ICourseService
{
    public async Task<CourseDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var course = await uow.Courses.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Course), id);
        return course.ToDto();
    }

    public async Task<PagedResult<CourseDto>> ListAsync(CourseListQuery query, CancellationToken ct = default)
    {
        var (items, total) = await uow.Courses.SearchAsync(
            query.Search?.Trim(), query.IsActive, query.Page, query.PageSize, ct);

        var dtos = items.Select(c => c.ToDto()).ToList();
        return new PagedResult<CourseDto>(dtos, total, query.Page, query.PageSize);
    }

    public async Task<CourseDto> CreateAsync(CreateCourseRequest request, CancellationToken ct = default)
    {
        var code = NormalizeCode(request.Code);
        if (await uow.Courses.CodeExistsAsync(code, null, ct))
            throw new ConflictException($"Course code '{code}' is already in use.");

        var course = new Course
        {
            Code = code,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Level = request.Level?.Trim(),
            DefaultDurationHours = request.DefaultDurationHours,
            DefaultPrice = request.DefaultPrice,
            IsActive = true
        };

        await uow.Courses.AddAsync(course, ct);
        await uow.SaveChangesAsync(ct);
        return course.ToDto();
    }

    public async Task<CourseDto> UpdateAsync(int id, UpdateCourseRequest request, CancellationToken ct = default)
    {
        var course = await uow.Courses.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Course), id);

        var code = NormalizeCode(request.Code);
        if (await uow.Courses.CodeExistsAsync(code, id, ct))
            throw new ConflictException($"Course code '{code}' is already in use.");

        course.Code = code;
        course.Name = request.Name.Trim();
        course.Description = request.Description?.Trim();
        course.Level = request.Level?.Trim();
        course.DefaultDurationHours = request.DefaultDurationHours;
        course.DefaultPrice = request.DefaultPrice;
        course.IsActive = request.IsActive;

        uow.Courses.Update(course);
        await uow.SaveChangesAsync(ct);
        return course.ToDto();
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var course = await uow.Courses.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Course), id);

        if (await uow.Courses.HasSectionsAsync(id, ct))
            throw new ConflictException("This course has sections and cannot be deleted. Deactivate it instead.");

        uow.Courses.Remove(course);
        await uow.SaveChangesAsync(ct);
    }

    private static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();
}
