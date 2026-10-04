using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Features.Sections;

public static class SectionMappings
{
    /// <remarks>Course, Trainer, Room and Schedules must be loaded.</remarks>
    public static SectionDto ToDto(this Section s) => new(
        s.Id,
        s.CourseId,
        s.Course.Name,
        s.TrainerId,
        s.Trainer.FullName,
        s.RoomId,
        s.Room.Name,
        s.Name,
        s.StartDate,
        s.EndDate,
        s.Price,
        s.Capacity,
        s.MinStudents,
        s.Status,
        s.Schedules
            .OrderBy(x => x.DayOfWeek).ThenBy(x => x.StartTime)
            .Select(x => new SectionScheduleDto(x.DayOfWeek, x.StartTime, x.EndTime))
            .ToList());
}
