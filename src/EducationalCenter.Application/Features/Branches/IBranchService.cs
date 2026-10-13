using EducationalCenter.Application.Common.Models;

namespace EducationalCenter.Application.Features.Branches;

public interface IBranchService
{
    Task<BranchDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PagedResult<BranchDto>> ListAsync(BranchListQuery query, CancellationToken ct = default);
    Task<BranchDto> CreateAsync(CreateBranchRequest request, CancellationToken ct = default);
    Task<BranchDto> UpdateAsync(int id, UpdateBranchRequest request, CancellationToken ct = default);

    /// <summary>Closing keeps all data but stops new records. The last active branch cannot be closed.</summary>
    Task<BranchDto> CloseAsync(int id, CancellationToken ct = default);

    Task<BranchDto> ReopenAsync(int id, CancellationToken ct = default);
}