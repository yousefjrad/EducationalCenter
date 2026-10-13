namespace EducationalCenter.Application.Features.Branches;

public interface IBranchLinkService
{
    Task<IReadOnlyList<BranchCourseDto>> ListCoursesAsync(int branchId, CancellationToken ct = default);
    Task<BranchCourseDto> EnableCourseAsync(int branchId, int courseId, CancellationToken ct = default);
    Task<BranchCourseDto> DisableCourseAsync(int branchId, int courseId, CancellationToken ct = default);

    Task<IReadOnlyList<BranchTrainerDto>> ListTrainersAsync(int branchId, CancellationToken ct = default);
    Task<BranchTrainerDto> AddTrainerAsync(int branchId, int trainerId, CancellationToken ct = default);
    Task RemoveTrainerAsync(int branchId, int trainerId, CancellationToken ct = default);
}