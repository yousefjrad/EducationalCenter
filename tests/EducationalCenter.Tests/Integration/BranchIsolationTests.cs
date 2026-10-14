using EducationalCenter.Application.Features.Branches;
using EducationalCenter.Application.Features.Expenses;
using EducationalCenter.Domain.Common;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Enums;
using EducationalCenter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EducationalCenter.Tests.Integration;

[Collection("integration")]
public sealed class BranchIsolationTests(AppFixture fx)
{
    private void SuperAdmin(int? branchId = null)
    {
        fx.Branch.IsSuperAdmin = true;
        fx.Branch.BranchId = branchId;
    }

    private void InBranch(int id)
    {
        fx.Branch.IsSuperAdmin = false;
        fx.Branch.BranchId = id;
    }

    private void Reset() => InBranch(1);

    private async Task<BranchDto> NewBranchAsync()
    {
        SuperAdmin();
        await using var scope = fx.NewScope();
        var code = ("T" + Guid.NewGuid().ToString("N")[..4]).ToUpperInvariant();
        return await scope.ServiceProvider.GetRequiredService<IBranchService>()
            .CreateAsync(new CreateBranchRequest("Iso " + code, code, null, null, null));
    }

    [Fact]
    public async Task Every_branch_owned_entity_has_a_query_filter_and_no_other_entity_carries_a_branch_by_accident()
    {
        await using var scope = fx.NewScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var owned = db.Model.GetEntityTypes().Where(t => typeof(IBranchOwned).IsAssignableFrom(t.ClrType)).ToList();
        Assert.NotEmpty(owned);

        foreach (var type in owned)
            Assert.True(type.GetQueryFilter() is not null, $"{type.ClrType.Name} is branch-owned but has no query filter.");

        // Entities that have a BranchId column without being branch-owned must be a known, deliberate exception.
        var allowed = new HashSet<string> { nameof(User), nameof(AuditLog), nameof(TrainerBranch), nameof(BranchCourse) };

        var stray = db.Model.GetEntityTypes()
            .Where(t => !typeof(IBranchOwned).IsAssignableFrom(t.ClrType)
                        && t.FindProperty("BranchId") is not null
                        && !allowed.Contains(t.ClrType.Name))
            .Select(t => t.ClrType.Name)
            .ToList();

        Assert.True(stray.Count == 0, "Entities with a BranchId that are not branch-owned: " + string.Join(", ", stray));
    }

    [Fact]
    public async Task Data_of_one_branch_is_invisible_to_another_branch_and_visible_to_the_super_admin()
    {
        try
        {
            var a = await NewBranchAsync();
            var b = await NewBranchAsync();
            var marker = "iso-" + Guid.NewGuid().ToString("N")[..8];

            InBranch(a.Id);
            int studentId, roomId, sectionId, enrollmentId;
            await using (var scope = fx.NewScope())
            {
                var seed = new Seeder(scope.ServiceProvider);
                var (course, trainer, room) = await seed.BaseAsync();
                var section = await seed.SectionAsync(course, trainer, room);
                var (student, enrollment) = await seed.EnrollAsync(section.Id);

                await scope.ServiceProvider.GetRequiredService<IExpenseService>().CreateAsync(
                    new CreateExpenseRequest("Test", marker, Currency.Syp, 500m, null, DateOnly.FromDateTime(DateTime.UtcNow)));

                studentId = student.Id;
                roomId = room.Id;
                sectionId = section.Id;
                enrollmentId = enrollment.Id;
            }

            async Task<(int Students, int Rooms, int Sections, int Enrollments, int Expenses)> CountAsync()
            {
                await using var scope = fx.NewScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                return (
                    await db.Students.CountAsync(x => x.Id == studentId),
                    await db.Rooms.CountAsync(x => x.Id == roomId),
                    await db.Sections.CountAsync(x => x.Id == sectionId),
                    await db.Enrollments.CountAsync(x => x.Id == enrollmentId),
                    await db.Expenses.CountAsync(x => x.Description == marker));
            }

            InBranch(a.Id);
            Assert.Equal((1, 1, 1, 1, 1), await CountAsync());

            InBranch(b.Id);
            Assert.Equal((0, 0, 0, 0, 0), await CountAsync());

            SuperAdmin(b.Id);
            Assert.Equal((0, 0, 0, 0, 0), await CountAsync());

            SuperAdmin(a.Id);
            Assert.Equal((1, 1, 1, 1, 1), await CountAsync());

            SuperAdmin();
            Assert.Equal((1, 1, 1, 1, 1), await CountAsync());
        }
        finally { Reset(); }
    }
}