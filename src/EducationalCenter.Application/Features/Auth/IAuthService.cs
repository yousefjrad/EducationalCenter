namespace EducationalCenter.Application.Features.Auth;

public interface IAuthService
{
    /// <summary>Wrong email, wrong password and inactive user all fail with the same message.</summary>
    Task<AuthResultDto> LoginAsync(LoginRequest request, CancellationToken ct = default);

    /// <summary>
    /// Rotates the refresh token: the old one is revoked and a new pair is issued.
    /// Presenting an already-revoked token ends every session of that user.
    /// </summary>
    Task<AuthResultDto> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default);

    /// <summary>Revokes the given refresh token. Unknown tokens are ignored.</summary>
    Task LogoutAsync(RefreshTokenRequest request, CancellationToken ct = default);

    /// <summary>The signed-in user changes their own password; all their sessions end.</summary>
    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default);

    Task<MeDto> GetCurrentUserAsync(CancellationToken ct = default);
}
