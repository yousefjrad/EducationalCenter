using EducationalCenter.Application.Features.ClassSessions;
using EducationalCenter.Application.Features.Enrollments;
using EducationalCenter.Application.Features.Sections;
using EducationalCenter.Application.Features.WaitingList;
using EducationalCenter.Domain.Enums;
using EducationalCenter.Domain.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace EducationalCenter.Tests.Integration;

[Collection("integration")]
public sealed class EnrollmentTests(AppFixture fx)
{
    [Fact]
    public async Task A_direct_enrollment_is_confirmed_at_the_section_price()
    {
        await using var scope = fx.NewScope();
        var seed = new Seeder(scope.ServiceProvider);
        var (course, trainer, room) = await seed.BaseAsync();
        var section = await seed.SectionAsync(course, trainer, room);

        var (_, enrollment) = await seed.EnrollAsync(section.Id);

        Assert.Equal(EnrollmentStatus.Confirmed, enrollment.Status);
        Assert.Equal(500_000m, enrollment.AgreedPrice);
    }

    [Fact]
    public async Task A_full_section_rejects_another_student()
    {
        await using var scope = fx.NewScope();
        var seed = new Seeder(scope.ServiceProvider);
        var (course, trainer, room) = await seed.BaseAsync();
        var section = await seed.SectionAsync(course, trainer, room, capacity: 1);
        await seed.EnrollAsync(section.Id);

        await Assert.ThrowsAsync<ConflictException>(() => seed.EnrollAsync(section.Id));
    }

    [Fact]
    public async Task A_student_cannot_enroll_twice_in_the_same_section()
    {
        await using var scope = fx.NewScope();
        var seed = new Seeder(scope.ServiceProvider);
        var (course, trainer, room) = await seed.BaseAsync();
        var section = await seed.SectionAsync(course, trainer, room);
        var enrollments = scope.ServiceProvider.GetRequiredService<IEnrollmentService>();
        var student = await seed.StudentAsync();
        await enrollments.CreateAsync(new CreateEnrollmentRequest(student.Id, section.Id));

        await Assert.ThrowsAsync<ConflictException>(
            () => enrollments.CreateAsync(new CreateEnrollmentRequest(student.Id, section.Id)));
    }

    [Fact]
    public async Task A_hold_is_pending_with_an_expiry_and_can_be_confirmed()
    {
        await using var scope = fx.NewScope();
        var seed = new Seeder(scope.ServiceProvider);
        var (course, trainer, room) = await seed.BaseAsync();
        var section = await seed.SectionAsync(course, trainer, room);
        var (_, hold) = await seed.EnrollAsync(section.Id, hold: true);

        var confirmed = await scope.ServiceProvider.GetRequiredService<IEnrollmentService>().ConfirmAsync(hold.Id);

        Assert.Equal(EnrollmentStatus.Pending, hold.Status);
        Assert.NotNull(hold.HoldExpiresAt);
        Assert.Equal(EnrollmentStatus.Confirmed, confirmed.Status);
    }

    [Fact]
    public async Task A_hold_takes_a_seat()
    {
        await using var scope = fx.NewScope();
        var seed = new Seeder(scope.ServiceProvider);
        var (course, trainer, room) = await seed.BaseAsync();
        var section = await seed.SectionAsync(course, trainer, room, capacity: 1);
        await seed.EnrollAsync(section.Id, hold: true);

        await Assert.ThrowsAsync<ConflictException>(() => seed.EnrollAsync(section.Id));
    }

    [Fact]
    public async Task An_expired_hold_releases_its_seat()
    {
        await using var scope = fx.NewScope();
        var seed = new Seeder(scope.ServiceProvider);
        var (course, trainer, room) = await seed.BaseAsync();
        var section = await seed.SectionAsync(course, trainer, room, capacity: 1);
        var enrollments = scope.ServiceProvider.GetRequiredService<IEnrollmentService>();
        var (_, hold) = await seed.EnrollAsync(section.Id, hold: true);

        try
        {
            fx.Clock.Jump(hold.HoldExpiresAt!.Value.AddMinutes(1));

            var expired = await enrollments.ExpireHoldsAsync();
            var after = await enrollments.GetByIdAsync(hold.Id);
            var (_, next) = await seed.EnrollAsync(section.Id);

            Assert.True(expired >= 1);
            Assert.NotEqual(EnrollmentStatus.Pending, after.Status);
            Assert.Equal(EnrollmentStatus.Confirmed, next.Status);
        }
        finally
        {
            fx.Clock.Reset();
        }
    }

    [Fact]
    public async Task Cancelling_an_enrollment_frees_the_seat()
    {
        await using var scope = fx.NewScope();
        var seed = new Seeder(scope.ServiceProvider);
        var (course, trainer, room) = await seed.BaseAsync();
        var section = await seed.SectionAsync(course, trainer, room, capacity: 1);
        var (_, first) = await seed.EnrollAsync(section.Id);
        var enrollments = scope.ServiceProvider.GetRequiredService<IEnrollmentService>();

        var cancelled = await enrollments.CancelAsync(first.Id);
        var (_, second) = await seed.EnrollAsync(section.Id);

        Assert.Equal(EnrollmentStatus.Cancelled, cancelled.Status);
        Assert.Equal(EnrollmentStatus.Confirmed, second.Status);
    }

    [Fact]
    public async Task The_waiting_list_keeps_its_order_and_promotes_into_a_free_seat()
    {
        await using var scope = fx.NewScope();
        var seed = new Seeder(scope.ServiceProvider);
        var (course, trainer, room) = await seed.BaseAsync();
        var section = await seed.SectionAsync(course, trainer, room, capacity: 1);
        var (_, first) = await seed.EnrollAsync(section.Id);
        var waiting = scope.ServiceProvider.GetRequiredService<IWaitingListService>();
        var studentB = await seed.StudentAsync();
        var studentC = await seed.StudentAsync();

        var entryB = await waiting.AddAsync(new AddToWaitingListRequest(studentB.Id, section.Id));
        var entryC = await waiting.AddAsync(new AddToWaitingListRequest(studentC.Id, section.Id));
        await scope.ServiceProvider.GetRequiredService<IEnrollmentService>().CancelAsync(first.Id);
        var promoted = await waiting.PromoteAsync(entryB.Id, new PromoteWaitingListEntryRequest());

        Assert.Equal(1, entryB.Position);
        Assert.Equal(2, entryC.Position);
        Assert.Equal(studentB.Id, promoted.StudentId);
        Assert.Equal(EnrollmentStatus.Confirmed, promoted.Status);
    }

    [Fact]
    public async Task A_transfer_moves_the_student_to_another_section_of_the_same_course()
    {
        await using var scope = fx.NewScope();
        var seed = new Seeder(scope.ServiceProvider);
        var (course, trainer, room) = await seed.BaseAsync();
        var from = await seed.SectionAsync(course, trainer, room);
        var to = await seed.SectionAsync(course, trainer, room, schedules: Seeder.SunTue("14:00", "16:00"));
        var (_, enrollment) = await seed.EnrollAsync(from.Id);
        var enrollments = scope.ServiceProvider.GetRequiredService<IEnrollmentService>();

        var moved = await enrollments.TransferAsync(enrollment.Id, new TransferEnrollmentRequest(to.Id));
        var old = await enrollments.GetByIdAsync(enrollment.Id);

        Assert.Equal(to.Id, moved.SectionId);
        Assert.Equal(EnrollmentStatus.Transferred, old.Status);
        await Assert.ThrowsAsync<ConflictException>(
            () => enrollments.TransferAsync(moved.Id, new TransferEnrollmentRequest(to.Id)));
    }

    [Fact]
    public async Task Completing_a_section_completes_its_confirmed_enrollments()
    {
        await using var scope = fx.NewScope();
        var seed = new Seeder(scope.ServiceProvider);
        var (course, trainer, room) = await seed.BaseAsync();
        var section = await seed.SectionAsync(course, trainer, room);
        var (_, enrollment) = await seed.EnrollAsync(section.Id);
        var sections = scope.ServiceProvider.GetRequiredService<ISectionService>();
        await scope.ServiceProvider.GetRequiredService<IClassSessionService>().GenerateForSectionAsync(section.Id);
        await sections.ChangeStatusAsync(section.Id, new ChangeSectionStatusRequest(SectionStatus.InProgress));

        await sections.ChangeStatusAsync(section.Id, new ChangeSectionStatusRequest(SectionStatus.Completed));
        var after = await scope.ServiceProvider.GetRequiredService<IEnrollmentService>().GetByIdAsync(enrollment.Id);

        Assert.Equal(EnrollmentStatus.Completed, after.Status);
    }
}