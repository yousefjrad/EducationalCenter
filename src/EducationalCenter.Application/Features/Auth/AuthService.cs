using EducationalCenter.Application.Common.Exceptions;
using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Common.Services;
using EducationalCenter.Application.Features.Users;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Exceptions;

namespace EducationalCenter.Application.Features.Auth;

public sealed class AuthService(
    IUnitOfWork uow,
    IClock clock,
    IPasswordHasher hasher,
    IAccessTokenGenerator accessTokens,
    ICurrentUser currentUser) : IAuthService
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);
    private const string InvalidCredentials = "Invalid email or password.";
    private const string InvalidRefreshToken = "Invalid refresh token.";

    public async Task<AuthResultDto> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await uow.Users.GetByEmailAsync(request.Email.Trim(), ct);

        if (user is null || !user.IsActive || !hasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException(InvalidCredentials);

        user.LastLoginAt = clock.UtcNow;

        var result = await IssueTokensAsync(user, ct);
        await uow.SaveChangesAsync(ct);
        return result;
    }

    public async Task<AuthResultDto> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var stored = await uow.RefreshTokens.GetByHashAsync(RefreshTokenCodec.Hash(request.RefreshToken), ct)
            ?? throw new UnauthorizedException(InvalidRefreshToken);

        var now = clock.UtcNow;

        if (stored.RevokedAt is not null)
        {
            // A revoked token came back: it may have been stolen, so end every session of this user.
            await RefreshTokenRevoker.RevokeAllAsync(uow, stored.UserId, now, ct);
            await uow.SaveChangesAsync(ct);
            throw new UnauthorizedException(InvalidRefreshToken);
        }

        if (stored.ExpiresAt <= now || !stored.User.IsActive)
            throw new UnauthorizedException(InvalidRefreshToken);

        stored.RevokedAt = now;

        var result = await IssueTokensAsync(stored.User, ct);
        await uow.SaveChangesAsync(ct);
        return result;
    }

    public async Task LogoutAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var stored = await uow.RefreshTokens.GetByHashAsync(RefreshTokenCodec.Hash(request.RefreshToken), ct);

        if (stored is not null && stored.RevokedAt is null)
        {
            stored.RevokedAt = clock.UtcNow;
            await uow.SaveChangesAsync(ct);
        }
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default)
    {
        var userId = currentUser.RequireUserId();
        var user = await uow.Users.GetWithRoleAsync(userId, ct) ?? throw new NotFoundException(nameof(User), userId);

        if (!hasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new RequestValidationException("currentPassword", "The current password is incorrect.");

        user.PasswordHash = hasher.Hash(request.NewPassword);
        await RefreshTokenRevoker.RevokeAllAsync(uow, user.Id, clock.UtcNow, ct);

        await uow.SaveChangesAsync(ct);
    }

    public async Task<MeDto> GetCurrentUserAsync(CancellationToken ct = default)
    {
        var userId = currentUser.RequireUserId();
        var user = await uow.Users.GetWithRoleAsync(userId, ct) ?? throw new NotFoundException(nameof(User), userId);
        var permissions = await uow.Users.GetPermissionNamesAsync(userId, ct);

        return new MeDto(user.ToDto(), permissions);
    }

    private async Task<AuthResultDto> IssueTokensAsync(User user, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var access = accessTokens.Generate(user, user.Role.Name);

        var rawRefresh = RefreshTokenCodec.Generate();
        var refreshExpiresAt = now.Add(RefreshTokenLifetime);

        await uow.RefreshTokens.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            User = user,
            TokenHash = RefreshTokenCodec.Hash(rawRefresh),
            ExpiresAt = refreshExpiresAt,
            CreatedAt = now
        }, ct);

        return new AuthResultDto(access.Token, access.ExpiresAtUtc, rawRefresh, refreshExpiresAt, user.ToDto());
    }
}
