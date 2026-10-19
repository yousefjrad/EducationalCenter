using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Common.Models;
using EducationalCenter.Application.Common.Services;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Exceptions;

namespace EducationalCenter.Application.Features.Courses;

/// <remarks>
/// Request-shape validation (FluentValidation) happens before this service, in an API filter.
/// Rules that need the database live here.
/// </remarks>
public sealed class CourseService(IUnitOfWork uow, IBranchContext branchContext, IAuditLogger audit) : ICourseService
{
    public async Task<CourseDto> GetByIdAsync(int id, CancellationToken ct = default) =>
        (await FindAsync(id, ct)).ToDto();

    public async Task<PagedResult<CourseDto>> ListAsync(CourseListQuery query, CancellationToken ct = default)
    {
        var (items, total) = branchContext.BranchId is int branchId
            ? await uow.Courses.SearchInBranchAsync(query.Search?.Trim(), query.IsActive, branchId, query.Page, query.PageSize, ct)
            : await uow.Courses.SearchAsync(query.Search?.Trim(), query.IsActive, query.Page, query.PageSize, ct);

        var branchIds = branchContext.IsSuperAdmin
            ? await uow.BranchLinks.GetCourseBranchIdsAsync(items.Select(c => c.Id).ToList(), ct)
            : new Dictionary<int, IReadOnlyList<int>>();

        var dtos = items.Select(c => c.ToDto(branchIds.TryGetValue(c.Id, out var b) ? b : null)).ToList();
        return new PagedResult<CourseDto>(dtos, total, query.Page, query.PageSize);
    }

    /// <remarks>A course created inside a branch is enabled for it automatically (see AppDbContext).</remarks>
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

        await audit.LogAsync("Course.Create", nameof(Course), course.Id, null, Snapshot(course), null, ct);
        await uow.SaveChangesAsync(ct);
        return course.ToDto();
    }

    public async Task<CourseDto> UpdateAsync(int id, UpdateCourseRequest request, CancellationToken ct = default)
    {
        var course = await FindAsync(id, ct);
        course.ExpectedRowVersion = RowVersionCodec.Decode(request.RowVersion);

        var code = NormalizeCode(request.Code);
        if (await uow.Courses.CodeExistsAsync(code, id, ct))
            throw new ConflictException($"Course code '{code}' is already in use.");

        var before = Snapshot(course);

        course.Code = code;
        course.Name = request.Name.Trim();
        course.Description = request.Description?.Trim();
        course.Level = request.Level?.Trim();
        course.DefaultDurationHours = request.DefaultDurationHours;
        course.DefaultPrice = request.DefaultPrice;
        course.IsActive = request.IsActive;

        await audit.LogAsync("Course.Update", nameof(Course), course.Id, before, Snapshot(course), null, ct);

        uow.Courses.Update(course);
        await uow.SaveChangesAsync(ct);
        return course.ToDto();
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var course = await FindAsync(id, ct);

        if (await uow.Courses.HasSectionsAsync(id, ct))
            throw new ConflictException("This course has sections and cannot be deleted. Deactivate it instead.");

        if (await uow.BranchLinks.IsCourseUsedByOtherBranchesAsync(id, branchContext.BranchId, ct))
            throw new ConflictException("This course is also used by other branches and cannot be deleted. Deactivate it instead.");

        await audit.LogAsync("Course.Delete", nameof(Course), course.Id, Snapshot(course), null, null, ct);

        uow.Courses.Remove(course);
        await uow.SaveChangesAsync(ct);
    }

    /// <summary>Inside a branch, a course the branch does not offer is reported as not found.</summary>
    private async Task<Course> FindAsync(int id, CancellationToken ct)
    {
        var course = await uow.Courses.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Course), id);

        if (branchContext.BranchId is int branchId && !await uow.BranchLinks.IsCourseLinkedAsync(id, branchId, ct))
            throw new NotFoundException(nameof(Course), id);

        return course;
    }

    private static object Snapshot(Course c) => new { c.Code, c.Name, c.Level, c.DefaultDurationHours, c.DefaultPrice, c.IsActive };

    private static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();
}