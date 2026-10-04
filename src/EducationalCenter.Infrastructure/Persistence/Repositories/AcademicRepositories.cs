using EducationalCenter.Application.Common.Interfaces.Repositories;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EducationalCenter.Infrastructure.Persistence.Repositories;

internal sealed class CourseRepository(AppDbContext context) : Repository<Course>(context), ICourseRepository
{
    public Task<bool> CodeExistsAsync(string code, int? excludeId, CancellationToken ct = default) =>
        Set.AnyAsync(c => c.Code == code && (excludeId == null || c.Id != excludeId), ct);

    public Task<bool> HasSectionsAsync(int courseId, CancellationToken ct = default) =>
        Db.Sections.AnyAsync(s => s.CourseId == courseId, ct);

    public Task<Course?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        Set.AsNoTracking().FirstOrDefaultAsync(c => c.Code == code, ct);

    public Task<(IReadOnlyList<Course> Items, int TotalCount)> SearchAsync(
        string? search, bool? isActive, int page, int pageSize, CancellationToken ct = default)
    {
        var query = Set.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Name.Contains(search) || c.Code.Contains(search));
        if (isActive.HasValue)
            query = query.Where(c => c.IsActive == isActive.Value);

        return query.OrderBy(c => c.Name).ThenBy(c => c.Id).ToPageAsync(page, pageSize, ct);
    }
}

internal sealed class RoomRepository(AppDbContext context) : Repository<Room>(context), IRoomRepository
{
    public Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken ct = default) =>
        Set.AnyAsync(r => r.Name == name && (excludeId == null || r.Id != excludeId), ct);

    public async Task<bool> IsInUseAsync(int roomId, CancellationToken ct = default) =>
        await Db.Sections.AnyAsync(s => s.RoomId == roomId, ct)
        || await Db.ClassSessions.AnyAsync(s => s.RoomId == roomId, ct);

    public Task<Room?> GetByNameAsync(string name, CancellationToken ct = default) =>
        Set.AsNoTracking().FirstOrDefaultAsync(r => r.Name == name, ct);

    public Task<(IReadOnlyList<Room> Items, int TotalCount)> SearchAsync(
        string? search, bool? isActive, int page, int pageSize, CancellationToken ct = default)
    {
        var query = Set.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(r => r.Name.Contains(search) || (r.Location != null && r.Location.Contains(search)));
        if (isActive.HasValue)
            query = query.Where(r => r.IsActive == isActive.Value);

        return query.OrderBy(r => r.Name).ThenBy(r => r.Id).ToPageAsync(page, pageSize, ct);
    }
}

internal sealed class TrainerRepository(AppDbContext context) : Repository<Trainer>(context), ITrainerRepository
{
    public async Task<bool> IsInUseAsync(int trainerId, CancellationToken ct = default) =>
        await Db.Sections.AnyAsync(s => s.TrainerId == trainerId, ct)
        || await Db.ClassSessions.AnyAsync(s => s.TrainerId == trainerId, ct)
        || await Db.TrainerPayrolls.AnyAsync(p => p.TrainerId == trainerId, ct);

    public async Task<IReadOnlyList<Trainer>> FindActiveByNameAsync(string fullName, CancellationToken ct = default) =>
        await Set.AsNoTracking().Where(t => t.IsActive && t.FullName == fullName).ToListAsync(ct);

    public Task<(IReadOnlyList<Trainer> Items, int TotalCount)> SearchAsync(
        string? search, bool? isActive, int page, int pageSize, CancellationToken ct = default)
    {
        var query = Set.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.FullName.Contains(search)
                                     || t.PhoneNumber.Contains(search)
                                     || (t.Specialty != null && t.Specialty.Contains(search)));
        if (isActive.HasValue)
            query = query.Where(t => t.IsActive == isActive.Value);

        return query.OrderBy(t => t.FullName).ThenBy(t => t.Id).ToPageAsync(page, pageSize, ct);
    }
}

internal sealed class SectionRepository(AppDbContext context) : Repository<Section>(context), ISectionRepository
{
    public Task<Section?> GetWithDetailsAsync(int id, CancellationToken ct = default) =>
        Set.Include(s => s.Course)
            .Include(s => s.Trainer)
            .Include(s => s.Room)
            .Include(s => s.Schedules)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<Section?> GetByNameAsync(int courseId, string name, CancellationToken ct = default) =>
        Set.AsNoTracking().FirstOrDefaultAsync(s => s.CourseId == courseId && s.Name == name, ct);

    public Task<bool> NameExistsAsync(int courseId, string name, int? excludeId, CancellationToken ct = default) =>
        Set.AnyAsync(s => s.CourseId == courseId && s.Name == name && (excludeId == null || s.Id != excludeId), ct);

    public Task<bool> HasSessionsAsync(int sectionId, CancellationToken ct = default) =>
        Db.ClassSessions.AnyAsync(c => c.SectionId == sectionId, ct);

    public async Task<bool> HasEnrollmentsAsync(int sectionId, CancellationToken ct = default) =>
        await Db.Enrollments.AnyAsync(e => e.SectionId == sectionId, ct)
        || await Db.WaitingListEntries.AnyAsync(w => w.SectionId == sectionId, ct);

    public void RemoveSchedules(IEnumerable<SectionSchedule> schedules) => Db.SectionSchedules.RemoveRange(schedules);

    public Task<(IReadOnlyList<Section> Items, int TotalCount)> SearchAsync(
        string? search, int? courseId, int? trainerId, int? roomId, SectionStatus? status,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = Set.AsNoTracking()
            .Include(s => s.Course)
            .Include(s => s.Trainer)
            .Include(s => s.Room)
            .Include(s => s.Schedules)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(s => s.Name.Contains(search) || s.Course.Name.Contains(search));
        if (courseId.HasValue)
            query = query.Where(s => s.CourseId == courseId.Value);
        if (trainerId.HasValue)
            query = query.Where(s => s.TrainerId == trainerId.Value);
        if (roomId.HasValue)
            query = query.Where(s => s.RoomId == roomId.Value);
        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        return query.OrderByDescending(s => s.StartDate).ThenBy(s => s.Name).ThenBy(s => s.Id).ToPageAsync(page, pageSize, ct);
    }
}

internal sealed class ClassSessionRepository(AppDbContext context) : Repository<ClassSession>(context), IClassSessionRepository
{
    public Task<ClassSession?> GetWithDetailsAsync(int id, CancellationToken ct = default) =>
        Set.Include(s => s.Section)
            .Include(s => s.Room)
            .Include(s => s.Trainer)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<ClassSession>> GetBySectionAsync(int sectionId, CancellationToken ct = default) =>
        await Set.Include(s => s.Section)
            .Include(s => s.Room)
            .Include(s => s.Trainer)
            .Where(s => s.SectionId == sectionId)
            .OrderBy(s => s.Date).ThenBy(s => s.StartTime)
            .ToListAsync(ct);

    public Task<bool> HasRoomConflictAsync(
        int roomId, DateOnly date, TimeOnly start, TimeOnly end, int? excludeSessionId, CancellationToken ct = default) =>
        Set.AnyAsync(s => s.RoomId == roomId
                          && s.Date == date
                          && (s.Status == ClassSessionStatus.Scheduled || s.Status == ClassSessionStatus.Held)
                          && s.StartTime < end && s.EndTime > start
                          && (excludeSessionId == null || s.Id != excludeSessionId), ct);

    public Task<bool> HasTrainerConflictAsync(
        int trainerId, DateOnly date, TimeOnly start, TimeOnly end, int? excludeSessionId, CancellationToken ct = default) =>
        Set.AnyAsync(s => s.TrainerId == trainerId
                          && s.Date == date
                          && (s.Status == ClassSessionStatus.Scheduled || s.Status == ClassSessionStatus.Held)
                          && s.StartTime < end && s.EndTime > start
                          && (excludeSessionId == null || s.Id != excludeSessionId), ct);

    public Task<(IReadOnlyList<ClassSession> Items, int TotalCount)> SearchAsync(
        int? sectionId, int? roomId, int? trainerId, DateOnly? from, DateOnly? to, ClassSessionStatus? status,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = Set.AsNoTracking()
            .Include(s => s.Section)
            .Include(s => s.Room)
            .Include(s => s.Trainer)
            .AsQueryable();

        if (sectionId.HasValue)
            query = query.Where(s => s.SectionId == sectionId.Value);
        if (roomId.HasValue)
            query = query.Where(s => s.RoomId == roomId.Value);
        if (trainerId.HasValue)
            query = query.Where(s => s.TrainerId == trainerId.Value);
        if (from.HasValue)
            query = query.Where(s => s.Date >= from.Value);
        if (to.HasValue)
            query = query.Where(s => s.Date <= to.Value);
        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        return query.OrderBy(s => s.Date).ThenBy(s => s.StartTime).ThenBy(s => s.Id).ToPageAsync(page, pageSize, ct);
    }

    public async Task<IReadOnlyList<ClassSession>> GetHeldByTrainerAsync(
        int trainerId, DateOnly from, DateOnly to, CancellationToken ct = default) =>
        await Set.AsNoTracking()
            .Where(s => s.TrainerId == trainerId
                        && s.Status == ClassSessionStatus.Held
                        && s.Date >= from && s.Date <= to)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ClassSession>> GetScheduleAsync(
        int? roomId, int? trainerId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var query = Set.AsNoTracking()
            .Include(s => s.Section).ThenInclude(x => x.Course)
            .Include(s => s.Room)
            .Include(s => s.Trainer)
            .Where(s => s.Date >= from && s.Date <= to);

        if (roomId.HasValue)
            query = query.Where(s => s.RoomId == roomId.Value);
        if (trainerId.HasValue)
            query = query.Where(s => s.TrainerId == trainerId.Value);

        return await query.OrderBy(s => s.Date).ThenBy(s => s.StartTime).ToListAsync(ct);
    }
}
