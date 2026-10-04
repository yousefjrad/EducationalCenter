using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Features.WaitingList;

public static class WaitingListMappings
{
    /// <remarks>Student and Section must be loaded.</remarks>
    public static WaitingListEntryDto ToDto(this WaitingListEntry e) => new(
        e.Id,
        e.StudentId,
        e.Student.FullName,
        e.Student.PhoneNumber,
        e.SectionId,
        e.Section.Name,
        e.Position,
        e.Status,
        e.AddedAt,
        e.PromotedEnrollmentId);
}
