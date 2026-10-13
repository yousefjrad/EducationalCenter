using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Exceptions;

namespace EducationalCenter.Application.Features.Branches;

public sealed class BranchLinkService(IUnitOfWork uow, IBranchContext branchContext, IAuditLogger audit) : IBranchLinkService
{
    public async Task<IReadOnlyList<BranchCourseDto>> ListCoursesAsync(int branchId, CancellationToken ct = default)
    {
        await GetVisibleBranchAsync(branchId, ct);
        var links = await uow.BranchLinks.GetCoursesAsync(branchId, ct);
        return links.OrderBy(l => l.Course.Name).Select(ToDto).ToList();
    }

    public async Task<BranchCourseDto> EnableCourseAsync(int branchId, int courseId, CancellationToken ct = default)
    {
        var branch = await GetVisibleBranchAsync(branchId, ct);
        if (!branch.IsActive)
            throw new ConflictException("This branch is closed.");

        var course = await uow.Courses.GetByIdAsync(courseId, ct) ?? throw new NotFoundException(nameof(Course), courseId);
        var link = await uow.BranchLinks.GetCourseLinkAsync(courseId, branchId, ct);

        if (link is null)
        {
            link = new BranchCourse { CourseId = course.Id, Course = course, BranchId = branchId, IsActive = true };
            await uow.BranchLinks.AddCourseLinkAsync(link, ct);
        }
        else
        {
            link.IsActive = true;
        }

        await audit.LogAsync("Branch.EnableCourse", nameof(Branch), branchId, null, new { courseId, course.Code }, null, ct);
        await uow.SaveChangesAsync(ct);
        return ToDto(link);
    }

    public async Task<BranchCourseDto> DisableCourseAsync(int branchId, int courseId, CancellationToken ct = default)
    {
        await GetVisibleBranchAsync(branchId, ct);

        var link = await uow.BranchLinks.GetCourseLinkAsync(courseId, branchId, ct)
            ?? throw new NotFoundException(nameof(BranchCourse), courseId);

        link.IsActive = false;

        await audit.LogAsync("Branch.DisableCourse", nameof(Branch), branchId, null, new { courseId, link.Course.Code }, null, ct);
        await uow.SaveChangesAsync(ct);
        return ToDto(link);
    }

    public async Task<IReadOnlyList<BranchTrainerDto>> ListTrainersAsync(int branchId, CancellationToken ct = default)
    {
        await GetVisibleBranchAsync(branchId, ct);
        var trainers = await uow.BranchLinks.GetTrainersAsync(branchId, ct);
        return trainers.OrderBy(t => t.FullName).Select(ToDto).ToList();
    }

    public async Task<BranchTrainerDto> AddTrainerAsync(int branchId, int trainerId, CancellationToken ct = default)
    {
        var branch = await GetVisibleBranchAsync(branchId, ct);
        if (!branch.IsActive)
            throw new ConflictException("This branch is closed.");

        var trainer = await uow.Trainers.GetByIdAsync(trainerId, ct) ?? throw new NotFoundException(nameof(Trainer), trainerId);

        if (await uow.BranchLinks.GetTrainerLinkAsync(trainerId, branchId, ct) is null)
        {
            await uow.BranchLinks.AddTrainerLinkAsync(new TrainerBranch { TrainerId = trainer.Id, Trainer = trainer, BranchId = branchId }, ct);
            await audit.LogAsync("Branch.AddTrainer", nameof(Branch), branchId, null, new { trainerId }, null, ct);
            await uow.SaveChangesAsync(ct);
        }

        return ToDto(trainer);
    }

    public async Task RemoveTrainerAsync(int branchId, int trainerId, CancellationToken ct = default)
    {
        await GetVisibleBranchAsync(branchId, ct);

        var link = await uow.BranchLinks.GetTrainerLinkAsync(trainerId, branchId, ct)
            ?? throw new NotFoundException(nameof(TrainerBranch), trainerId);

        if (await uow.BranchLinks.TrainerHasWorkInBranchAsync(trainerId, branchId, ct))
            throw new ConflictException("This trainer has sections, sessions or payroll in this branch and cannot be unlinked from it.");

        uow.BranchLinks.RemoveTrainerLink(link);
        await audit.LogAsync("Branch.RemoveTrainer", nameof(Branch), branchId, new { trainerId }, null, null, ct);
        await uow.SaveChangesAsync(ct);
    }

    /// <summary>A branch outside the caller's scope is reported as not found.</summary>
    private async Task<Branch> GetVisibleBranchAsync(int id, CancellationToken ct)
    {
        var branch = await uow.Branches.GetByIdAsync(id, ct);
        if (branch is null || (!branchContext.IsSuperAdmin && branch.Id != branchContext.BranchId))
            throw new NotFoundException(nameof(Branch), id);
        return branch;
    }

    private static BranchCourseDto ToDto(BranchCourse l) =>
        new(l.CourseId, l.Course.Code, l.Course.Name, l.Course.DefaultPrice, l.Course.IsActive, l.IsActive);

    private static BranchTrainerDto ToDto(Trainer t) => new(t.Id, t.FullName, t.Specialty, t.IsActive);
}