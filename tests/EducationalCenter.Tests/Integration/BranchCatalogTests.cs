using EducationalCenter.Application.Features.Branches;
using EducationalCenter.Application.Features.Trainers;
using EducationalCenter.Domain.Enums;
using EducationalCenter.Domain.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace EducationalCenter.Tests.Integration;

[Collection("integration")]
public sealed class BranchCatalogTests(AppFixture fx)
{
    private static string NewCode() => ("T" + Guid.NewGuid().ToString("N")[..4]).ToUpperInvariant();

    private void SuperAdmin()
    {
        fx.Branch.IsSuperAdmin = true;
        fx.Branch.BranchId = null;
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
        var code = NewCode();
        return await scope.ServiceProvider.GetRequiredService<IBranchService>()
            .CreateAsync(new CreateBranchRequest("Cat " + code, code, null, null, null));
    }

    private async Task RunLinksAsync(Func<IBranchLinkService, Task> action)
    {
        await using var scope = fx.NewScope();
        await action(scope.ServiceProvider.GetRequiredService<IBranchLinkService>());
    }

    [Fact]
    public async Task A_trainer_is_visible_only_in_the_branches_they_are_linked_to()
    {
        try
        {
            var b2 = await NewBranchAsync();

            InBranch(1);
            await using var scope = fx.NewScope();
            var trainers = scope.ServiceProvider.GetRequiredService<ITrainerService>();
            var name = "Linked " + Guid.NewGuid().ToString("N")[..8];
            var phone = "+963" + Random.Shared.Next(900_000_000, 999_999_999);
            var trainer = await trainers.CreateAsync(new CreateTrainerRequest(name, phone, null, TrainerPayType.Monthly, 1000m, Currency.Syp));
            Assert.Equal(1, (await trainers.ListAsync(new TrainerListQuery(name))).TotalCount);

            InBranch(b2.Id);
            Assert.Equal(0, (await trainers.ListAsync(new TrainerListQuery(name))).TotalCount);
            await Assert.ThrowsAsync<NotFoundException>(() => trainers.GetByIdAsync(trainer.Id));

            SuperAdmin();
            await RunLinksAsync(l => l.AddTrainerAsync(b2.Id, trainer.Id));
            InBranch(b2.Id);
            Assert.Equal(1, (await trainers.ListAsync(new TrainerListQuery(name))).TotalCount);

            SuperAdmin();
            await RunLinksAsync(l => l.RemoveTrainerAsync(b2.Id, trainer.Id));
            InBranch(b2.Id);
            Assert.Equal(0, (await trainers.ListAsync(new TrainerListQuery(name))).TotalCount);
        }
        finally { Reset(); }
    }

    [Fact]
    public async Task A_trainer_with_work_in_a_branch_cannot_be_unlinked_from_it()
    {
        try
        {
            InBranch(1);
            await using var scope = fx.NewScope();
            var seeder = new Seeder(scope.ServiceProvider);
            var (course, trainer, room) = await seeder.BaseAsync();
            await seeder.SectionAsync(course, trainer, room);

            SuperAdmin();
            await Assert.ThrowsAsync<ConflictException>(() => RunLinksAsync(l => l.RemoveTrainerAsync(1, trainer.Id)));
        }
        finally { Reset(); }
    }

    [Fact]
    public async Task A_section_needs_the_course_and_trainer_to_be_offered_in_its_branch()
    {
        try
        {
            var b2 = await NewBranchAsync();

            InBranch(1);
            await using var scope = fx.NewScope();
            var seeder = new Seeder(scope.ServiceProvider);
            var course = await seeder.CourseAsync();
            var trainer = await seeder.TrainerAsync();

            InBranch(b2.Id);
            var room = await seeder.RoomAsync();

            // Neither the course nor the trainer belongs to branch 2 yet.
            await Assert.ThrowsAsync<ConflictException>(() => seeder.SectionAsync(course, trainer, room));

            SuperAdmin();
            await RunLinksAsync(l => l.AddTrainerAsync(b2.Id, trainer.Id));
            InBranch(b2.Id);
            await Assert.ThrowsAsync<ConflictException>(() => seeder.SectionAsync(course, trainer, room));

            SuperAdmin();
            await RunLinksAsync(l => l.EnableCourseAsync(b2.Id, course.Id));
            InBranch(b2.Id);
            var section = await seeder.SectionAsync(course, trainer, room);
            Assert.Equal(course.Id, section.CourseId);

            SuperAdmin();
            await RunLinksAsync(l => l.DisableCourseAsync(b2.Id, course.Id));
            InBranch(b2.Id);
            await Assert.ThrowsAsync<ConflictException>(() => seeder.SectionAsync(course, trainer, room, name: "Again " + Guid.NewGuid().ToString("N")[..6]));
        }
        finally { Reset(); }
    }
}