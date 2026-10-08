using System.Collections.Concurrent;
using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Features.Auth;
using EducationalCenter.Application.Features.Roles;
using EducationalCenter.Application.Features.Users;
using EducationalCenter.Domain.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace EducationalCenter.Tests.Integration;

/// <summary>Remembers the security events so a test can check what was recorded.</summary>
public sealed class FakeSecurityEvents : ISecurityEventLogger
{
    private readonly ConcurrentQueue<string> _events = new();

    public void Record(string eventName, int? userId, string? email, string? details) =>
        _events.Enqueue(eventName + "|" + email);

    public bool Has(string eventName, string email) => _events.Contains(eventName + "|" + email);
}

[Collection("integration")]
public sealed class LoginLockoutTests(AppFixture fx)
{
    private const string Password = "Right-Pass-123";
    private const string Wrong = "Wrong-Pass-000";

    private async Task<string> NewUserAsync()
    {
        await using var scope = fx.NewScope();
        var roles = await scope.ServiceProvider.GetRequiredService<IRoleService>().ListAsync();
        var email = "lock." + Guid.NewGuid().ToString("N")[..10] + "@tests.local";
        await scope.ServiceProvider.GetRequiredService<IUserService>()
            .CreateAsync(new CreateUserRequest("Lock Test", email, Password, roles[0].Id));
        return email;
    }

    private async Task<AuthResultDto> LoginAsync(string email, string password)
    {
        await using var scope = fx.NewScope();
        return await scope.ServiceProvider.GetRequiredService<IAuthService>()
            .LoginAsync(new LoginRequest(email, password));
    }

    private async Task FailAsync(string email, int times)
    {
        for (var i = 0; i < times; i++)
            await Assert.ThrowsAsync<UnauthorizedException>(() => LoginAsync(email, Wrong));
    }

    [Fact]
    public async Task Five_wrong_passwords_lock_the_account_even_for_the_right_password()
    {
        var email = await NewUserAsync();

        await FailAsync(email, AuthService.MaxFailedAttempts);
        await Assert.ThrowsAsync<UnauthorizedException>(() => LoginAsync(email, Password));

        Assert.True(fx.Security.Has(SecurityEvents.AccountLocked, email));
        Assert.True(fx.Security.Has(SecurityEvents.LoginBlocked, email));
    }

    [Fact]
    public async Task A_successful_sign_in_resets_the_failure_counter()
    {
        var email = await NewUserAsync();

        await FailAsync(email, AuthService.MaxFailedAttempts - 1);
        await LoginAsync(email, Password);
        await FailAsync(email, AuthService.MaxFailedAttempts - 1);
        var result = await LoginAsync(email, Password);

        Assert.NotEmpty(result.AccessToken);
    }

    [Fact]
    public async Task The_lock_ends_after_the_lockout_period()
    {
        var email = await NewUserAsync();
        await FailAsync(email, AuthService.MaxFailedAttempts);

        try
        {
            fx.Clock.Jump(DateTime.UtcNow.Add(AuthService.LockoutDuration).AddMinutes(1));

            var result = await LoginAsync(email, Password);

            Assert.NotEmpty(result.AccessToken);
        }
        finally
        {
            fx.Clock.Reset();
        }
    }

    [Fact]
    public async Task An_unknown_email_and_a_wrong_password_get_the_same_message()
    {
        var email = await NewUserAsync();

        var wrongPassword = await Assert.ThrowsAsync<UnauthorizedException>(() => LoginAsync(email, Wrong));
        var unknownEmail = await Assert.ThrowsAsync<UnauthorizedException>(() => LoginAsync("nobody@tests.local", Wrong));

        Assert.Equal(wrongPassword.Message, unknownEmail.Message);
    }
}