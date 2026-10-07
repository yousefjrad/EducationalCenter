using EducationalCenter.Application.Features.Courses;
using EducationalCenter.Application.Features.Enrollments;
using EducationalCenter.Application.Features.Rooms;
using EducationalCenter.Application.Features.Sections;
using EducationalCenter.Application.Features.Students;
using EducationalCenter.Application.Features.Trainers;
using EducationalCenter.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace EducationalCenter.Tests.Integration;

/// <summary>Creates test data through the real services, with unique names so tests never collide.</summary>
public sealed class Seeder(IServiceProvider sp)
{
    private static int _counter;

    public static readonly DateOnly Start = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);
    public static readonly DateOnly End = Start.AddDays(27);

    private static int Next() => Interlocked.Increment(ref _counter);

    private static string Stamp() => Next().ToString("D4") + DateTime.UtcNow.ToString("HHmmss");

    private static string Phone() => "+963" + (900_000_000 + Next());

    public static SectionScheduleDto Slot(DayOfWeek day, string from, string to) =>
        new(day, TimeOnly.Parse(from), TimeOnly.Parse(to));

    public static IReadOnlyList<SectionScheduleDto> SunTue(string from = "09:00", string to = "11:00") =>
        new[] { Slot(DayOfWeek.Sunday, from, to), Slot(DayOfWeek.Tuesday, from, to) };

    public Task<CourseDto> CourseAsync(decimal price = 500_000m) =>
        sp.GetRequiredService<ICourseService>().CreateAsync(
            new CreateCourseRequest("C" + Stamp(), "Course " + Stamp(), null, null, 40, price));

    public Task<RoomDto> RoomAsync(int capacity = 20) =>
        sp.GetRequiredService<IRoomService>().CreateAsync(
            new CreateRoomRequest("Room " + Stamp(), capacity, null, null));

    public Task<TrainerDto> TrainerAsync() =>
        sp.GetRequiredService<ITrainerService>().CreateAsync(
            new CreateTrainerRequest("Trainer " + Stamp(), Phone(), null, TrainerPayType.Monthly, 3_000_000m, Currency.Syp));

    public Task<StudentDto> StudentAsync() =>
        sp.GetRequiredService<IStudentService>().CreateAsync(
            new CreateStudentRequest("Student " + Stamp(), Phone()));

    public async Task<(CourseDto Course, TrainerDto Trainer, RoomDto Room)> BaseAsync(int roomCapacity = 20) =>
        (await CourseAsync(), await TrainerAsync(), await RoomAsync(roomCapacity));

    /// <summary>A section priced 500000 that is open for enrollment unless open is false.</summary>
    public async Task<SectionDto> SectionAsync(
        CourseDto course, TrainerDto trainer, RoomDto room, int capacity = 10,
        IReadOnlyList<SectionScheduleDto>? schedules = null, bool open = true, string? name = null)
    {
        var sections = sp.GetRequiredService<ISectionService>();
        var section = await sections.CreateAsync(new CreateSectionRequest(
            course.Id, trainer.Id, room.Id, name ?? "S" + Stamp(), Start, End, 500_000m, capacity, 1, schedules ?? SunTue()));

        return open
            ? await sections.ChangeStatusAsync(section.Id, new ChangeSectionStatusRequest(SectionStatus.OpenForEnrollment))
            : section;
    }

    public async Task<(StudentDto Student, EnrollmentDto Enrollment)> EnrollAsync(int sectionId, bool hold = false)
    {
        var student = await StudentAsync();
        var enrollment = await sp.GetRequiredService<IEnrollmentService>()
            .CreateAsync(new CreateEnrollmentRequest(student.Id, sectionId, hold));
        return (student, enrollment);
    }
}