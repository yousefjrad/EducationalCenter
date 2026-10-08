using EducationalCenter.Application.Features.Auth;
using EducationalCenter.Application.Features.Roles;
using EducationalCenter.Application.Features.Users;
using EducationalCenter.Domain.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace EducationalCenter.Tests.Integration;

[Collection("integration")]
public sealed class AdminUnlockTests(AppFixture fx)
{
    private const string Password = "Right-Pass-123";
    private const string Wrong = "Wrong-Pass-000";

    private async Task<UserDto> NewUserAsync()
    {
        await using var scope = fx.NewScope();
        var roles = await scope.ServiceProvider.GetRequiredService<IRoleService>().ListAsync();
        var role = roles.First(r => r.Name != "Admin");
        var email = "unlock." + Guid.NewGuid().ToString("N")[..10] + "@tests.local";
        return await scope.ServiceProvider.GetRequiredService<IUserService>()
            .CreateAsync(new CreateUserRequest("Unlock Test", email, Password, role.Id));
    }

    private async Task<AuthResultDto> LoginAsync(string email, string password)
    {
        await using var scope = fx.NewScope();
        return await scope.ServiceProvider.GetRequiredService<IAuthService>()
            .LoginAsync(new LoginRequest(email, password));
    }

    private async Task<UserDto> LockedUserAsync()
    {
        var user = await NewUserAsync();
        for (var i = 0; i < AuthService.MaxFailedAttempts; i++)
            await Assert.ThrowsAsync<UnauthorizedException>(() => LoginAsync(user.Email, Wrong));

        await Assert.ThrowsAsync<UnauthorizedException>(() => LoginAsync(user.Email, Password));
        return user;
    }

    private async Task<T> WithUsersAsync<T>(Func<IUserService, Task<T>> action)
    {
        await using var scope = fx.NewScope();
        return await action(scope.ServiceProvider.GetRequiredService<IUserService>());
    }

    [Fact]
    public async Task The_user_shows_when_the_lock_ends()
    {
        var user = await LockedUserAsync();

        var shown = await WithUsersAsync(users => users.GetByIdAsync(user.Id));

        Assert.NotNull(shown.LockedUntil);
        Assert.True(shown.LockedUntil > DateTime.UtcNow);
    }

    [Fact]
    public async Task An_admin_can_unlock_a_user_immediately()
    {
        var user = await LockedUserAsync();

        var unlocked = await WithUsersAsync(users => users.UnlockAsync(user.Id));
        var result = await LoginAsync(user.Email, Password);

        Assert.Null(unlocked.LockedUntil);
        Assert.NotEmpty(result.AccessToken);
    }

    [Fact]
    public async Task Resetting_the_password_clears_the_lock()
    {
        var user = await LockedUserAsync();

        await WithUsersAsync(async users =>
        {
            await users.ResetPasswordAsync(user.Id, new ResetPasswordRequest("New-Pass-456"));
            return 0;
        });
        var result = await LoginAsync(user.Email, "New-Pass-456");

        Assert.NotEmpty(result.AccessToken);
    }

    [Fact]
    public async Task Reactivating_a_user_clears_the_lock()
    {
        var user = await LockedUserAsync();

        await WithUsersAsync(users => users.UpdateAsync(user.Id, new UpdateUserRequest(user.FullName, user.Email, user.RoleId, false)));
        await WithUsersAsync(users => users.UpdateAsync(user.Id, new UpdateUserRequest(user.FullName, user.Email, user.RoleId, true)));
        var result = await LoginAsync(user.Email, Password);

        Assert.NotEmpty(result.AccessToken);
    }
}