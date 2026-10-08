using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Common.Models;
using EducationalCenter.Application.Common.Services;
using EducationalCenter.Domain.Constants;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Exceptions;

namespace EducationalCenter.Application.Features.Users;

public sealed class UserService(
    IUnitOfWork uow,
    IClock clock,
    IPasswordHasher hasher,
    ICurrentUser currentUser) : IUserService
{
    public async Task<UserDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var user = await uow.Users.GetWithRoleAsync(id, ct) ?? throw new NotFoundException(nameof(User), id);
        return user.ToDto();
    }

    public async Task<PagedResult<UserDto>> ListAsync(UserListQuery query, CancellationToken ct = default)
    {
        var (items, total) = await uow.Users.SearchAsync(
            query.Search?.Trim(), query.RoleId, query.IsActive, query.Page, query.PageSize, ct);

        return new PagedResult<UserDto>(items.Select(u => u.ToDto()).ToList(), total, query.Page, query.PageSize);
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var email = NormalizeEmail(request.Email);
        if (await uow.Users.EmailExistsAsync(email, null, ct))
            throw new ConflictException($"The email '{email}' is already in use.");

        var role = await uow.Roles.GetByIdAsync(request.RoleId, ct)
            ?? throw new NotFoundException(nameof(Role), request.RoleId);

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = hasher.Hash(request.Password),
            RoleId = role.Id,
            Role = role,
            IsActive = true
        };

        await uow.Users.AddAsync(user, ct);
        await uow.SaveChangesAsync(ct);
        return user.ToDto();
    }

    public async Task<UserDto> UpdateAsync(int id, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await uow.Users.GetWithRoleAsync(id, ct) ?? throw new NotFoundException(nameof(User), id);

        var email = NormalizeEmail(request.Email);
        if (!string.Equals(email, user.Email, StringComparison.OrdinalIgnoreCase)
            && await uow.Users.EmailExistsAsync(email, id, ct))
            throw new ConflictException($"The email '{email}' is already in use.");

        var role = request.RoleId == user.RoleId
            ? user.Role
            : await uow.Roles.GetByIdAsync(request.RoleId, ct) ?? throw new NotFoundException(nameof(Role), request.RoleId);

        if (user.Id == currentUser.UserId && !request.IsActive)
            throw new ConflictException("You cannot deactivate your own account.");

        await EnsureNotLastAdminAsync(user, role, request.IsActive, ct);

        var sessionsMustEnd = (user.IsActive && !request.IsActive) || user.RoleId != role.Id;

        var reactivated = !user.IsActive && request.IsActive;

        user.FullName = request.FullName.Trim();
        user.Email = email;
        user.RoleId = role.Id;
        user.Role = role;
        user.IsActive = request.IsActive;

        // Reactivating an account also ends any failed-sign-in lock.
        if (reactivated)
        {
            user.FailedLoginCount = 0;
            user.LockedUntil = null;
        }

        if (sessionsMustEnd)
            await RefreshTokenRevoker.RevokeAllAsync(uow, user.Id, clock.UtcNow, ct);

        await uow.SaveChangesAsync(ct);
        return user.ToDto();
    }

    public async Task<UserDto> UnlockAsync(int id, CancellationToken ct = default)
    {
        var user = await uow.Users.GetWithRoleAsync(id, ct) ?? throw new NotFoundException(nameof(User), id);

        user.FailedLoginCount = 0;
        user.LockedUntil = null;
        await uow.SaveChangesAsync(ct);

        return user.ToDto();
    }

    public async Task ResetPasswordAsync(int id, ResetPasswordRequest request, CancellationToken ct = default)
    {
        var user = await uow.Users.GetWithRoleAsync(id, ct) ?? throw new NotFoundException(nameof(User), id);

        // A new password also ends any failed-sign-in lock.
        user.FailedLoginCount = 0;
        user.LockedUntil = null;

        user.PasswordHash = hasher.Hash(request.NewPassword);
        await RefreshTokenRevoker.RevokeAllAsync(uow, user.Id, clock.UtcNow, ct);

        await uow.SaveChangesAsync(ct);
    }

    /// <summary>The system must always keep at least one active Admin.</summary>
    private async Task EnsureNotLastAdminAsync(User user, Role newRole, bool newIsActive, CancellationToken ct)
    {
        var isActiveAdminNow = user.IsActive && user.Role.Name == SystemRoles.Admin;
        var staysActiveAdmin = newIsActive && newRole.Name == SystemRoles.Admin;

        if (isActiveAdminNow && !staysActiveAdmin)
        {
            var activeAdmins = await uow.Users.CountActiveInRoleAsync(user.RoleId, ct);
            if (activeAdmins <= 1)
                throw new ConflictException("The last active Admin cannot be deactivated or moved to another role.");
        }
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
