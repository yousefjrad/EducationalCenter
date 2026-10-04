using EducationalCenter.Application.Common.Models;

namespace EducationalCenter.Application.Features.Users;

public interface IUserService
{
    Task<UserDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PagedResult<UserDto>> ListAsync(UserListQuery query, CancellationToken ct = default);
    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken ct = default);

    /// <summary>
    /// Users are never deleted (the audit log refers to them); they are deactivated.
    /// Deactivating a user or changing their role ends their sessions.
    /// </summary>
    Task<UserDto> UpdateAsync(int id, UpdateUserRequest request, CancellationToken ct = default);

    Task ResetPasswordAsync(int id, ResetPasswordRequest request, CancellationToken ct = default);
}
