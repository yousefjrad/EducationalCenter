namespace EducationalCenter.Application.Features.Branches;

public sealed record BranchDto(int Id, string Name, string Code, string? Governorate, string? Address, string? Phone, bool IsActive, DateTime CreatedAt);

public sealed record CreateBranchRequest(string Name, string Code, string? Governorate, string? Address, string? Phone);

public sealed record UpdateBranchRequest(string Name, string? Governorate, string? Address, string? Phone);