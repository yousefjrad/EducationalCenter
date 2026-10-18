using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Common.Interfaces.Repositories;

/// <summary>Which trainers teach at which branch, and which catalog courses a branch offers.</summary>
public interface IBranchLinkRepository
{
    Task<bool> IsTrainerInBranchAsync(int trainerId, int branchId, CancellationToken ct = default);

    /// <summary>The course is linked to the branch (active or not): the branch may see it.</summary>
    Task<bool> IsCourseLinkedAsync(int courseId, int branchId, CancellationToken ct = default);

    /// <summary>The course is linked and active: the branch may open new sections of it.</summary>
    Task<bool> IsCourseOfferedAsync(int courseId, int branchId, CancellationToken ct = default);

    Task<IReadOnlyList<BranchCourse>> GetCoursesAsync(int branchId, CancellationToken ct = default);
    Task<BranchCourse?> GetCourseLinkAsync(int courseId, int branchId, CancellationToken ct = default);
    Task AddCourseLinkAsync(BranchCourse link, CancellationToken ct = default);

    Task<IReadOnlyList<Trainer>> GetTrainersAsync(int branchId, CancellationToken ct = default);
    Task<TrainerBranch?> GetTrainerLinkAsync(int trainerId, int branchId, CancellationToken ct = default);
    Task AddTrainerLinkAsync(TrainerBranch link, CancellationToken ct = default);
    void RemoveTrainerLink(TrainerBranch link);

    /// <summary>The trainer has sections, sessions or payroll in that branch (so the link must stay).</summary>
    Task<bool> TrainerHasWorkInBranchAsync(int trainerId, int branchId, CancellationToken ct = default);

    /// <summary>The branches each trainer teaches at (trainer id to branch ids).</summary>
    Task<IReadOnlyDictionary<int, IReadOnlyList<int>>> GetTrainerBranchIdsAsync(IReadOnlyCollection<int> trainerIds, CancellationToken ct = default);

    /// <summary>The branches that offer each course (active links only).</summary>
    Task<IReadOnlyDictionary<int, IReadOnlyList<int>>> GetCourseBranchIdsAsync(IReadOnlyCollection<int> courseIds, CancellationToken ct = default);

    /// <summary>The course is active in another branch, or has sections in another branch (a null branch means: any branch).</summary>
    Task<bool> IsCourseUsedByOtherBranchesAsync(int courseId, int? exceptBranchId, CancellationToken ct = default);

    /// <summary>The trainer is linked to another branch (a null branch means: any branch).</summary>
    Task<bool> IsTrainerUsedByOtherBranchesAsync(int trainerId, int? exceptBranchId, CancellationToken ct = default);
}