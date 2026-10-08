using EducationalCenter.Application.Features.Enrollments;
using EducationalCenter.Domain.Enums;
using EducationalCenter.Domain.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace EducationalCenter.Tests.Integration;

[Collection("integration")]
public sealed class ConcurrencyTests(AppFixture fx)
{
    /// <summary>Lets many students enroll at the same moment, each in its own scope, and returns how many got a seat.</summary>
    private async Task<int> RaceAsync(int capacity, int students)
    {
        await using var setup = fx.NewScope();
        var seed = new Seeder(setup.ServiceProvider);
        var (course, trainer, room) = await seed.BaseAsync(roomCapacity: Math.Max(capacity, 20));
        var section = await seed.SectionAsync(course, trainer, room, capacity: capacity);

        var ids = new List<int>();
        for (var i = 0; i < students; i++)
            ids.Add((await seed.StudentAsync()).Id);

        var results = await Task.WhenAll(ids.Select(async id =>
        {
            await using var scope = fx.NewScope();
            try
            {
                await scope.ServiceProvider.GetRequiredService<IEnrollmentService>()
                    .CreateAsync(new CreateEnrollmentRequest(id, section.Id));
                return true;
            }
            catch (ConflictException)
            {
                return false;
            }
        }));

        var stored = await setup.ServiceProvider.GetRequiredService<IEnrollmentService>()
            .ListAsync(new EnrollmentListQuery(SectionId: section.Id, Status: EnrollmentStatus.Confirmed, PageSize: 100));

        var winners = results.Count(r => r);
        Assert.Equal(winners, stored.TotalCount);
        return winners;
    }

    [Fact]
    public async Task Concurrent_enrollments_never_sell_the_last_seat_twice()
    {
        for (var round = 0; round < 3; round++)
            Assert.Equal(1, await RaceAsync(capacity: 1, students: 8));
    }

    [Fact]
    public async Task Concurrent_enrollments_fill_exactly_the_capacity() =>
        Assert.Equal(3, await RaceAsync(capacity: 3, students: 10));
}