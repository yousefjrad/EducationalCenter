using EducationalCenter.Application.Common.Exceptions;
using EducationalCenter.Application.Features.Branches;
using EducationalCenter.Application.Features.Roles;
using EducationalCenter.Application.Features.Users;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Exceptions;
using EducationalCenter.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace EducationalCenter.Tests.Integration;

[Collection("integration")]
public sealed class BranchManagementTests(AppFixture fx)
{
    private static string NewCode() => ("T" + Guid.NewGuid().ToString("N")[..4]).ToUpperInvariant();

    private async Task<T> InBranchesAsync<T>(Func<IBranchService, Task<T>> action)
    {
        await using var scope = fx.NewScope();
        return await action(scope.ServiceProvider.GetRequiredService<IBranchService>());
    }

    private async Task<T> InUsersAsync<T>(Func<IUserService, Task<T>> action)
    {
        await using var scope = fx.NewScope();
        return await action(scope.ServiceProvider.GetRequiredService<IUserService>());
    }

    private void SuperAdmin()
    {
        fx.Branch.IsSuperAdmin = true;
        fx.Branch.BranchId = null;
    }

    private void Reset()
    {
        fx.Branch.IsSuperAdmin = false;
        fx.Branch.BranchId = 1;
    }

    [Fact]
    public async Task A_branch_code_is_uppercased_and_must_be_unique()
    {
        try
        {
            SuperAdmin();
            var code = NewCode();

            var created = await InBranchesAsync(s => s.CreateAsync(new CreateBranchRequest("Test " + code, code.ToLowerInvariant(), "Gov", null, null)));
            Assert.Equal(code, created.Code);
            Assert.True(created.IsActive);

            await Assert.ThrowsAsync<ConflictException>(() =>
                InBranchesAsync(s => s.CreateAsync(new CreateBranchRequest("Again", code, null, null, null))));
        }
        finally { Reset(); }
    }

    [Fact]
    public async Task A_closed_branch_accepts_no_new_records_until_it_is_reopened()
    {
        try
        {
            SuperAdmin();
            var code = NewCode();
            var branch = await InBranchesAsync(s => s.CreateAsync(new CreateBranchRequest("Closing " + code, code, null, null, null)));

            var closed = await InBranchesAsync(s => s.CloseAsync(branch.Id));
            Assert.False(closed.IsActive);

            fx.Branch.BranchId = branch.Id;
            await using (var scope = fx.NewScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Students.Add(new Student { FullName = "Closed branch student", PhoneNumber = "09" + Guid.NewGuid().ToString("N")[..8] });
                await Assert.ThrowsAsync<ConflictException>(() => db.SaveChangesAsync());
            }

            fx.Branch.BranchId = null;
            var reopened = await InBranchesAsync(s => s.ReopenAsync(branch.Id));
            Assert.True(reopened.IsActive);
        }
        finally { Reset(); }
    }

    [Fact]
    public async Task A_branch_manager_sees_only_their_own_branch()
    {
        try
        {
            SuperAdmin();
            var code = NewCode();
            var other = await InBranchesAsync(s => s.CreateAsync(new CreateBranchRequest("Other " + code, code, null, null, null)));

            Reset();
            var list = await InBranchesAsync(s => s.ListAsync());
            Assert.Single(list);
            Assert.Equal(1, list[0].Id);

            await Assert.ThrowsAsync<NotFoundException>(() => InBranchesAsync(s => s.GetByIdAsync(other.Id)));
        }
        finally { Reset(); }
    }

    [Fact]
    public async Task Only_the_admin_has_no_branch_and_every_other_user_needs_one()
    {
        try
        {
            SuperAdmin();
            List<RoleDto> roles;
            await using (var scope = fx.NewScope())
                roles = (await scope.ServiceProvider.GetRequiredService<IRoleService>().ListAsync()).ToList();

            var admin = roles.First(r => r.Name == "Admin");
            var receptionist = roles.First(r => r.Name == "Receptionist");
            string Email() => "branch." + Guid.NewGuid().ToString("N")[..10] + "@tests.local";

            await Assert.ThrowsAsync<RequestValidationException>(() =>
                InUsersAsync(u => u.CreateAsync(new CreateUserRequest("Admin With Branch", Email(), "Right-Pass-123", admin.Id, 1))));

            await Assert.ThrowsAsync<RequestValidationException>(() =>
                InUsersAsync(u => u.CreateAsync(new CreateUserRequest("No Branch", Email(), "Right-Pass-123", receptionist.Id))));

            var ok = await InUsersAsync(u => u.CreateAsync(new CreateUserRequest("With Branch", Email(), "Right-Pass-123", receptionist.Id, 1)));
            Assert.Equal(1, ok.BranchId);
        }
        finally { Reset(); }
    }
}