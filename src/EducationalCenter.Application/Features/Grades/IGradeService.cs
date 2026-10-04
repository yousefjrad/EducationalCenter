namespace EducationalCenter.Application.Features.Grades;

public interface IGradeService
{
    /// <summary>Creates or replaces the final grade. Blocked once a certificate has been issued.</summary>
    Task<GradeDto> RecordAsync(RecordGradeRequest request, CancellationToken ct = default);

    Task<GradeDto> GetByEnrollmentAsync(int enrollmentId, CancellationToken ct = default);
    Task<IReadOnlyList<GradeDto>> ListBySectionAsync(int sectionId, CancellationToken ct = default);
}
