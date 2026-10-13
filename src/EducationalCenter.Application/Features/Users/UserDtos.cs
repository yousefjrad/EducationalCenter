namespace EducationalCenter.Application.Features.Users;

public sealed record UserDto(
    int Id,
    string FullName,
    string Email,
    int RoleId,
    string RoleName,
    bool IsActive,
    DateTime? LastLoginAt,
    DateTime? LockedUntil,
    int? BranchId = null);

public sealed record CreateUserRequest(string FullName, string Email, string Password, int RoleId, int? BranchId = null);

public sealed record UpdateUserRequest(string FullName, string Email, int RoleId, bool IsActive, int? BranchId = null);

/// <summary>Admin sets a new password for another user (their sessions are ended).</summary>
public sealed record ResetPasswordRequest(string NewPassword);

public sealed record UserListQuery(
    string? Search = null,
    int? RoleId = null,
    bool? IsActive = null,
    int Page = 1,
    int PageSize = 20,
    int? BranchId = null);
