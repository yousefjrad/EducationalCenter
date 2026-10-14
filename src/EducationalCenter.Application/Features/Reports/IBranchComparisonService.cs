namespace EducationalCenter.Application.Features.Reports;

public interface IBranchComparisonService
{
    /// <summary>All branches side by side (the current branch scope does not hide any). Needs the cross-branch reports permission.</summary>
    Task<BranchComparisonReportDto> GetAsync(BranchComparisonQuery query, CancellationToken ct = default);
}