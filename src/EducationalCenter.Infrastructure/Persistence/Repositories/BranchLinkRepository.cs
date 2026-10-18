using EducationalCenter.Application.Common.Interfaces.Repositories;
using EducationalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EducationalCenter.Infrastructure.Persistence.Repositories;

internal sealed class BranchLinkRepository(AppDbContext db) : IBranchLinkRepository
{
    public Task<bool> IsTrainerInBranchAsync(int trainerId, int branchId, CancellationToken ct = default) =>
        db.TrainerBranches.AnyAsync(x => x.TrainerId == trainerId && x.BranchId == branchId, ct);

    public Task<bool> IsCourseLinkedAsync(int courseId, int branchId, CancellationToken ct = default) =>
        db.BranchCourses.AnyAsync(x => x.CourseId == courseId && x.BranchId == branchId, ct);

    public Task<bool> IsCourseOfferedAsync(int courseId, int branchId, CancellationToken ct = default) =>
        db.BranchCourses.AnyAsync(x => x.CourseId == courseId && x.BranchId == branchId && x.IsActive, ct);

    public async Task<IReadOnlyList<BranchCourse>> GetCoursesAsync(int branchId, CancellationToken ct = default) =>
        await db.BranchCourses.AsNoTracking().Include(x => x.Course).Where(x => x.BranchId == branchId).ToListAsync(ct);

    public Task<BranchCourse?> GetCourseLinkAsync(int courseId, int branchId, CancellationToken ct = default) =>
        db.BranchCourses.Include(x => x.Course).FirstOrDefaultAsync(x => x.CourseId == courseId && x.BranchId == branchId, ct);

    public async Task AddCourseLinkAsync(BranchCourse link, CancellationToken ct = default) =>
        await db.BranchCourses.AddAsync(link, ct);

    public async Task<IReadOnlyList<Trainer>> GetTrainersAsync(int branchId, CancellationToken ct = default) =>
        await db.TrainerBranches.AsNoTracking().Where(x => x.BranchId == branchId).Select(x => x.Trainer).ToListAsync(ct);

    public Task<TrainerBranch?> GetTrainerLinkAsync(int trainerId, int branchId, CancellationToken ct = default) =>
        db.TrainerBranches.Include(x => x.Trainer).FirstOrDefaultAsync(x => x.TrainerId == trainerId && x.BranchId == branchId, ct);

    public async Task AddTrainerLinkAsync(TrainerBranch link, CancellationToken ct = default) =>
        await db.TrainerBranches.AddAsync(link, ct);

    public async Task<bool> IsCourseUsedByOtherBranchesAsync(int courseId, int? exceptBranchId, CancellationToken ct = default) =>
        await db.BranchCourses.AnyAsync(x => x.CourseId == courseId && x.IsActive && (exceptBranchId == null || x.BranchId != exceptBranchId), ct)
        || await db.Sections.IgnoreQueryFilters().AnyAsync(s => s.CourseId == courseId && !s.IsDeleted && (exceptBranchId == null || s.BranchId != exceptBranchId), ct);

    public Task<bool> IsTrainerUsedByOtherBranchesAsync(int trainerId, int? exceptBranchId, CancellationToken ct = default) =>
        db.TrainerBranches.AnyAsync(x => x.TrainerId == trainerId && (exceptBranchId == null || x.BranchId != exceptBranchId), ct);

    public void RemoveTrainerLink(TrainerBranch link) => db.TrainerBranches.Remove(link);

    public async Task<IReadOnlyDictionary<int, IReadOnlyList<int>>> GetTrainerBranchIdsAsync(IReadOnlyCollection<int> trainerIds, CancellationToken ct = default)
    {
        var rows = await db.TrainerBranches.AsNoTracking()
            .Where(x => trainerIds.Contains(x.TrainerId))
            .Select(x => new { x.TrainerId, x.BranchId })
            .ToListAsync(ct);
        return rows.GroupBy(x => x.TrainerId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<int>)g.Select(x => x.BranchId).OrderBy(b => b).ToList());
    }

    public async Task<IReadOnlyDictionary<int, IReadOnlyList<int>>> GetCourseBranchIdsAsync(IReadOnlyCollection<int> courseIds, CancellationToken ct = default)
    {
        var rows = await db.BranchCourses.AsNoTracking()
            .Where(x => x.IsActive && courseIds.Contains(x.CourseId))
            .Select(x => new { x.CourseId, x.BranchId })
            .ToListAsync(ct);
        return rows.GroupBy(x => x.CourseId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<int>)g.Select(x => x.BranchId).OrderBy(b => b).ToList());
    }

    public async Task<bool> TrainerHasWorkInBranchAsync(int trainerId, int branchId, CancellationToken ct = default) =>
        await db.Sections.IgnoreQueryFilters().AnyAsync(s => s.TrainerId == trainerId && s.BranchId == branchId && !s.IsDeleted, ct)
        || await db.ClassSessions.IgnoreQueryFilters().AnyAsync(s => s.TrainerId == trainerId && s.BranchId == branchId && !s.IsDeleted, ct)
        || await db.TrainerPayrolls.IgnoreQueryFilters().AnyAsync(p => p.TrainerId == trainerId && p.BranchId == branchId && !p.IsDeleted, ct);
}