using EducationalCenter.Application.Features.PublicCatalog;
using EducationalCenter.Application.Features.Sections;
using EducationalCenter.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EducationalCenter.Infrastructure.Persistence;

internal sealed class PublicCatalogReader(AppDbContext db) : IPublicCatalogReader
{
    public async Task<IReadOnlyList<PublicSectionDto>> GetOpenSectionsAsync(DateTime utcNow, CancellationToken ct = default)
    {
        var rows = await db.Sections.AsNoTracking()
            .Where(s => s.Status == SectionStatus.OpenForEnrollment)
            .OrderBy(s => s.StartDate)
            .Select(s => new
            {
                s.Id,
                CourseName = s.Course.Name,
                s.Name,
                s.StartDate,
                s.EndDate,
                s.Price,
                s.Capacity,
                Taken = s.Enrollments.Count(e =>
                    e.Status == EnrollmentStatus.Confirmed
                    || e.Status == EnrollmentStatus.Completed
                    || (e.Status == EnrollmentStatus.Pending && e.HoldExpiresAt > utcNow)),
                Schedules = s.Schedules
                    .OrderBy(x => x.DayOfWeek).ThenBy(x => x.StartTime)
                    .Select(x => new SectionScheduleDto(x.DayOfWeek, x.StartTime, x.EndTime))
            })
            .ToListAsync(ct);

        return rows.Select(r => new PublicSectionDto(
            r.Id,
            r.CourseName,
            r.Name,
            r.StartDate,
            r.EndDate,
            r.Price,
            Math.Max(0, r.Capacity - r.Taken),
            r.Schedules.ToList())).ToList();
    }
}