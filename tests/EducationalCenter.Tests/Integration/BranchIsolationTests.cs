using EducationalCenter.Application.Common.Exceptions;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EducationalCenter.Tests.Integration;

[Collection("integration")]
public sealed class BranchIsolationTests(AppFixture fx)
{
    [Fact]
    public async Task Records_get_the_request_branch_and_are_hidden_from_other_branches()
    {
        var phone = "09" + Guid.NewGuid().ToString("N")[..8];

        try
        {
            // A second branch (Branch itself is not branch-owned, so any scope can create it).
            fx.Branch.BranchId = null;
            int branch2;
            await using (var scope = fx.NewScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var branch = new Branch { Name = "Second", Code = "T" + Guid.NewGuid().ToString("N")[..4].ToUpperInvariant() };
                db.Branches.Add(branch);
                await db.SaveChangesAsync();
                branch2 = branch.Id;
            }

            // A student created in branch 1 is stamped with branch 1, whatever the code sets.
            fx.Branch.BranchId = 1;
            await using (var scope = fx.NewScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Students.Add(new Student { FullName = "Branch One Student", PhoneNumber = phone, BranchId = branch2 });
                await db.SaveChangesAsync();

                var stored = await db.Students.SingleAsync(s => s.PhoneNumber == phone);
                Assert.Equal(1, stored.BranchId);
            }

            // Branch 2 cannot see it.
            fx.Branch.BranchId = branch2;
            await using (var scope = fx.NewScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                Assert.Equal(0, await db.Students.CountAsync(s => s.PhoneNumber == phone));
            }

            // A user with no branch sees nothing and cannot write.
            fx.Branch.BranchId = -1;
            await using (var scope = fx.NewScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                Assert.Equal(0, await db.Students.CountAsync(s => s.PhoneNumber == phone));

                db.Students.Add(new Student { FullName = "Nobody", PhoneNumber = phone });
                await Assert.ThrowsAsync<RequestValidationException>(() => db.SaveChangesAsync());
            }

            // No scope (Super Admin, all branches) sees it, but cannot create a record without choosing a branch.
            fx.Branch.BranchId = null;
            await using (var scope = fx.NewScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                Assert.Equal(1, await db.Students.CountAsync(s => s.PhoneNumber == phone));

                db.Students.Add(new Student { FullName = "No branch chosen", PhoneNumber = phone });
                await Assert.ThrowsAsync<RequestValidationException>(() => db.SaveChangesAsync());
            }
        }
        finally
        {
            fx.Branch.BranchId = 1;
        }
    }
}