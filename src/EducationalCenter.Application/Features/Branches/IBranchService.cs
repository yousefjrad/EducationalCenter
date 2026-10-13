namespace EducationalCenter.Application.Features.Branches;

public interface IBranchService
{
    Task<IReadOnlyList<BranchDto>> ListAsync(CancellationToken ct = default);
    Task<BranchDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<BranchDto> CreateAsync(CreateBranchRequest request, CancellationToken ct = default);
    Task<BranchDto> UpdateAsync(int id, UpdateBranchRequest request, CancellationToken ct = default);
    Task<BranchDto> CloseAsync(int id, CancellationToken ct = default);
    Task<BranchDto> ReopenAsync(int id, CancellationToken ct = default);
}