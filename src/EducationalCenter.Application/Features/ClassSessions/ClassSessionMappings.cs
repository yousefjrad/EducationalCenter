using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Features.ClassSessions;

public static class ClassSessionMappings
{
    /// <remarks>Section, Room and Trainer must be loaded.</remarks>
    public static ClassSessionDto ToDto(this ClassSession s) => new(
        s.Id,
        s.SectionId,
        s.Section.Name,
        s.Date,
        s.StartTime,
        s.EndTime,
        s.RoomId,
        s.Room.Name,
        s.TrainerId,
        s.Trainer.FullName,
        s.Status,
        s.Notes);
}
