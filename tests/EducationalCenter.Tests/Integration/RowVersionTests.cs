using EducationalCenter.Application.Features.Students;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EducationalCenter.Tests.Integration;

[Collection("integration")]
public sealed class RowVersionTests(AppFixture fx)
{
    private async Task<T> RunAsync<T>(Func<IStudentService, Task<T>> action)
    {
        await using var scope = fx.NewScope();
        return await action(scope.ServiceProvider.GetRequiredService<IStudentService>());
    }

    [Fact]
    public async Task Saving_with_an_outdated_version_is_refused_and_a_current_one_is_accepted()
    {
        fx.Branch.IsSuperAdmin = false;
        fx.Branch.BranchId = 1;

        var name = "Ver " + Guid.NewGuid().ToString("N")[..8];
        var phone = "+963" + Random.Shared.Next(900_000_000, 999_999_999);

        var created = await RunAsync(s => s.CreateAsync(new CreateStudentRequest(name, phone)));
        Assert.False(string.IsNullOrEmpty(created.RowVersion));

        // Both people opened the record in version "created".
        var first = await RunAsync(s => s.UpdateAsync(created.Id, new UpdateStudentRequest(name + " A", phone, true, created.RowVersion)));
        Assert.NotEqual(created.RowVersion, first.RowVersion);

        // The second one still holds the old version: refused, and the first change survives.
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            RunAsync(s => s.UpdateAsync(created.Id, new UpdateStudentRequest(name + " B", phone, true, created.RowVersion))));
        Assert.Equal(name + " A", (await RunAsync(s => s.GetByIdAsync(created.Id))).FullName);

        // After reloading (current version) the save goes through.
        var second = await RunAsync(s => s.UpdateAsync(created.Id, new UpdateStudentRequest(name + " B", phone, true, first.RowVersion)));
        Assert.Equal(name + " B", second.FullName);
    }

    [Fact]
    public async Task A_request_without_a_version_still_saves()
    {
        fx.Branch.IsSuperAdmin = false;
        fx.Branch.BranchId = 1;

        var name = "NoVer " + Guid.NewGuid().ToString("N")[..8];
        var phone = "+963" + Random.Shared.Next(900_000_000, 999_999_999);

        var created = await RunAsync(s => s.CreateAsync(new CreateStudentRequest(name, phone)));
        var updated = await RunAsync(s => s.UpdateAsync(created.Id, new UpdateStudentRequest(name + " X", phone, true)));
        Assert.Equal(name + " X", updated.FullName);
    }
}