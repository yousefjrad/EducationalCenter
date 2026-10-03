using EducationalCenter.Domain.Enums;

namespace EducationalCenter.Application.Features.Sections;

public sealed record SectionScheduleDto(DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime);

public sealed record SectionDto(
    int Id,
    int CourseId,
    string CourseName,
    int TrainerId,
    string TrainerName,
    int RoomId,
    string RoomName,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal Price,
    int Capacity,
    int MinStudents,
    SectionStatus Status,
    IReadOnlyList<SectionScheduleDto> Schedules);

/// <param name="Price">Optional: defaults to the course's default price.</param>
public sealed record CreateSectionRequest(
    int CourseId,
    int TrainerId,
    int RoomId,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal? Price,
    int Capacity,
    int MinStudents,
    IReadOnlyList<SectionScheduleDto> Schedules);

/// <remarks>
/// Trainer, room, dates and weekly schedule can only change while the section has no
/// sessions and no enrollments. Name, price, capacity and min students can always change.
/// </remarks>
public sealed record UpdateSectionRequest(
    int TrainerId,
    int RoomId,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal Price,
    int Capacity,
    int MinStudents,
    IReadOnlyList<SectionScheduleDto> Schedules);

public sealed record ChangeSectionStatusRequest(SectionStatus Status);

public sealed record SectionListQuery(
    string? Search = null,
    int? CourseId = null,
    int? TrainerId = null,
    int? RoomId = null,
    SectionStatus? Status = null,
    int Page = 1,
    int PageSize = 20);
