using Asp.Versioning;
using EducationalCenter.Application.Features.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EducationalCenter.API.Controllers;

[ApiVersion("1.0")]
public sealed class AuthController(IAuthService auth) : ApiControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<AuthResultDto>> Login(LoginRequest request, CancellationToken ct) =>
        Ok(await auth.LoginAsync(request, ct));

    /// <summary>Exchanges a refresh token for a new access token and a new refresh token.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<AuthResultDto>> Refresh(RefreshTokenRequest request, CancellationToken ct) =>
        Ok(await auth.RefreshAsync(request, ct));

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(RefreshTokenRequest request, CancellationToken ct)
    {
        await auth.LogoutAsync(request, ct);
        return NoContent();
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        await auth.ChangePasswordAsync(request, ct);
        return NoContent();
    }

    /// <summary>The signed-in user and the permissions of their role.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<MeDto>> Me(CancellationToken ct) =>
        Ok(await auth.GetCurrentUserAsync(ct));
}
