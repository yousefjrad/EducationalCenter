using EducationalCenter.Application.Features.ClassSessions;
using EducationalCenter.Application.Features.Sections;
using EducationalCenter.Domain.Enums;
using EducationalCenter.Domain.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace EducationalCenter.Tests.Integration;

[Collection("integration")]
public sealed class AcademicRulesTests(AppFixture fx)
{
    private static int CountDays(DateOnly from, DateOnly to, params DayOfWeek[] days)
    {
        var count = 0;
        for (var day = from; day <= to; day = day.AddDays(1))
            if (days.Contains(day.DayOfWeek))
                count++;
        return count;
    }

    private static ChangeSectionStatusRequest To(SectionStatus status) => new(status);

    [Fact]
    public async Task A_section_cannot_be_larger_than_its_room()
    {
        await using var scope = fx.NewScope();
        var seed = new Seeder(scope.ServiceProvider);
        var (course, trainer, room) = await seed.BaseAsync(roomCapacity: 10);

        await Assert.ThrowsAsync<ConflictException>(
            () => seed.SectionAsync(course, trainer, room, capacity: 11, open: false));
    }

    [Fact]
    public async Task Two_sections_of_one_course_cannot_share_a_name()
    {
        await using var scope = fx.NewScope();
        var seed = new Seeder(scope.ServiceProvider);
        var (course, trainer, room) = await seed.BaseAsync();
        await seed.SectionAsync(course, trainer, room, name: "Same name", open: false);

        await Assert.ThrowsAsync<ConflictException>(
            () => seed.SectionAsync(course, trainer, room, name: "Same name", open: false));
    }

    [Fact]
    public async Task Generating_sessions_creates_one_session_per_scheduled_day()
    {
        await using var scope = fx.NewScope();
        var seed = new Seeder(scope.ServiceProvider);
        var (course, trainer, room) = await seed.BaseAsync();
        var section = await seed.SectionAsync(course, trainer, room);

        var sessions = await scope.ServiceProvider.GetRequiredService<IClassSessionService>()
            .GenerateForSectionAsync(section.Id);

        Assert.Equal(CountDays(Seeder.Start, Seeder.End, DayOfWeek.Sunday, DayOfWeek.Tuesday), sessions.Count);
        Assert.All(sessions, s => Assert.Contains(s.Date.DayOfWeek, new[] { DayOfWeek.Sunday, DayOfWeek.Tuesday }));
    }

    [Fact]
    public async Task Sessions_are_generated_only_once_per_section()
    {
        await using var scope = fx.NewScope();
        var seed = new Seeder(scope.ServiceProvider);
        var (course, trainer, room) = await seed.BaseAsync();
        var section = await seed.SectionAsync(course, trainer, room);
        var generator = scope.ServiceProvider.GetRequiredService<IClassSessionService>();
        await generator.GenerateForSectionAsync(section.Id);

        await Assert.ThrowsAsync<ConflictException>(() => generator.GenerateForSectionAsync(section.Id));
    }

    [Fact]
    public async Task Two_sections_cannot_use_the_same_room_at_the_same_time()
    {
        await using var scope = fx.NewScope();
        var seed = new Seeder(scope.ServiceProvider);
        var (course, trainerA, room) = await seed.BaseAsync();
        var trainerB = await seed.TrainerAsync();
        var first = await seed.SectionAsync(course, trainerA, room);
        var second = await seed.SectionAsync(course, trainerB, room);
        var generator = scope.ServiceProvider.GetRequiredService<IClassSessionService>();
        await generator.GenerateForSectionAsync(first.Id);

        await Assert.ThrowsAsync<ConflictException>(() => generator.GenerateForSectionAsync(second.Id));
    }

    [Fact]
    public async Task One_trainer_cannot_teach_two_sections_at_the_same_time()
    {
        await using var scope = fx.NewScope();
        var seed = new Seeder(scope.ServiceProvider);
        var (course, trainer, roomA) = await seed.BaseAsync();
        var roomB = await seed.RoomAsync();
        var first = await seed.SectionAsync(course, trainer, roomA);
        var second = await seed.SectionAsync(course, trainer, roomB);
        var generator = scope.ServiceProvider.GetRequiredService<IClassSessionService>();
        await generator.GenerateForSectionAsync(first.Id);

        await Assert.ThrowsAsync<ConflictException>(() => generator.GenerateForSectionAsync(second.Id));
    }

    [Fact]
    public async Task Partially_overlapping_hours_in_the_same_room_conflict()
    {
        await using var scope = fx.NewScope();
        var seed = new Seeder(scope.ServiceProvider);
        var (course, trainerA, room) = await seed.BaseAsync();
        var trainerB = await seed.TrainerAsync();
        var first = await seed.SectionAsync(course, trainerA, room);
        var second = await seed.SectionAsync(course, trainerB, room, schedules: Seeder.SunTue("10:00", "12:00"));
        var generator = scope.ServiceProvider.GetRequiredService<IClassSessionService>();
        await generator.GenerateForSectionAsync(first.Id);

        await Assert.ThrowsAsync<ConflictException>(() => generator.GenerateForSectionAsync(second.Id));
    }

    [Fact]
    public async Task The_same_room_at_different_hours_is_fine()
    {
        await using var scope = fx.NewScope();
        var seed = new Seeder(scope.ServiceProvider);
        var (course, trainerA, room) = await seed.BaseAsync();
        var trainerB = await seed.TrainerAsync();
        var first = await seed.SectionAsync(course, trainerA, room);
        var second = await seed.SectionAsync(course, trainerB, room, schedules: Seeder.SunTue("14:00", "16:00"));
        var generator = scope.ServiceProvider.GetRequiredService<IClassSessionService>();
        await generator.GenerateForSectionAsync(first.Id);

        var sessions = await generator.GenerateForSectionAsync(second.Id);

        Assert.NotEmpty(sessions);
    }

    [Fact]
    public async Task Back_to_back_classes_in_the_same_room_do_not_conflict()
    {
        await using var scope = fx.NewScope();
        var seed = new Seeder(scope.ServiceProvider);
        var (course, trainerA, room) = await seed.BaseAsync();
        var trainerB = await seed.TrainerAsync();
        var first = await seed.SectionAsync(course, trainerA, room);
        var second = await seed.SectionAsync(course, trainerB, room, schedules: Seeder.SunTue("11:00", "13:00"));
        var generator = scope.ServiceProvider.GetRequiredService<IClassSessionService>();
        await generator.GenerateForSectionAsync(first.Id);

        var sessions = await generator.GenerateForSectionAsync(second.Id);

        Assert.NotEmpty(sessions);
    }

    [Fact]
    public async Task A_draft_section_cannot_jump_to_completed()
    {
        await using var scope = fx.NewScope();
        var seed = new Seeder(scope.ServiceProvider);
        var (course, trainer, room) = await seed.BaseAsync();
        var section = await seed.SectionAsync(course, trainer, room, open: false);

        await Assert.ThrowsAsync<ConflictException>(
            () => scope.ServiceProvider.GetRequiredService<ISectionService>()
                .ChangeStatusAsync(section.Id, To(SectionStatus.Completed)));
    }

    [Fact]
    public async Task A_section_cannot_start_without_sessions()
    {
        await using var scope = fx.NewScope();
        var seed = new Seeder(scope.ServiceProvider);
        var (course, trainer, room) = await seed.BaseAsync();
        var section = await seed.SectionAsync(course, trainer, room);

        await Assert.ThrowsAsync<ConflictException>(
            () => scope.ServiceProvider.GetRequiredService<ISectionService>()
                .ChangeStatusAsync(section.Id, To(SectionStatus.InProgress)));
    }

    [Fact]
    public async Task A_section_moves_through_its_whole_lifecycle()
    {
        await using var scope = fx.NewScope();
        var seed = new Seeder(scope.ServiceProvider);
        var (course, trainer, room) = await seed.BaseAsync();
        var section = await seed.SectionAsync(course, trainer, room);
        var sections = scope.ServiceProvider.GetRequiredService<ISectionService>();
        await scope.ServiceProvider.GetRequiredService<IClassSessionService>().GenerateForSectionAsync(section.Id);

        var running = await sections.ChangeStatusAsync(section.Id, To(SectionStatus.InProgress));
        var done = await sections.ChangeStatusAsync(section.Id, To(SectionStatus.Completed));

        Assert.Equal(SectionStatus.InProgress, running.Status);
        Assert.Equal(SectionStatus.Completed, done.Status);
    }

    [Fact]
    public async Task A_cancelled_section_is_final()
    {
        await using var scope = fx.NewScope();
        var seed = new Seeder(scope.ServiceProvider);
        var (course, trainer, room) = await seed.BaseAsync();
        var section = await seed.SectionAsync(course, trainer, room);
        var sections = scope.ServiceProvider.GetRequiredService<ISectionService>();
        await sections.ChangeStatusAsync(section.Id, To(SectionStatus.Cancelled));

        await Assert.ThrowsAsync<ConflictException>(() => sections.ChangeStatusAsync(section.Id, To(SectionStatus.Cancelled)));
        await Assert.ThrowsAsync<ConflictException>(() => sections.ChangeStatusAsync(section.Id, To(SectionStatus.OpenForEnrollment)));
    }
}