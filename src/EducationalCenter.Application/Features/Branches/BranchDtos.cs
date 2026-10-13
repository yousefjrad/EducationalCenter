namespace EducationalCenter.Application.Features.Branches;

public sealed record BranchDto(int Id, string Code, string Name, string? Governorate, string? Address, string? Phone, bool IsActive, DateTime CreatedAt);

public sealed record CreateBranchRequest(string Code, string Name, string? Governorate, string? Address, string? Phone);

public sealed record UpdateBranchRequest(string Name, string? Governorate, string? Address, string? Phone);

public sealed record BranchListQuery(string? Search = null, bool? IsActive = null, int Page = 1, int PageSize = 20);