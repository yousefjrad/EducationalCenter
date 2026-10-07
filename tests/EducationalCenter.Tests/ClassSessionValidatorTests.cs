using EducationalCenter.Application.Features.ClassSessions;

namespace EducationalCenter.Tests;

public class ClassSessionValidatorTests
{
    private readonly CreateClassSessionRequestValidator _create = new();
    private readonly PostponeClassSessionRequestValidator _postpone = new();

    private static CreateClassSessionRequest Req(
        DateOnly? date = null, string from = "09:00", string to = "11:00",
        int sectionId = 1, int? roomId = null, int? trainerId = null, string? notes = null) =>
        new(sectionId, date ?? new DateOnly(2026, 11, 1), TimeOnly.Parse(from), TimeOnly.Parse(to), roomId, trainerId, notes);

    [Fact]
    public void A_complete_request_is_valid() => Check.Valid(_create.Validate(Req()));

    [Fact]
    public void Room_and_trainer_are_optional_and_default_to_the_section() =>
        Check.Valid(_create.Validate(Req(roomId: null, trainerId: null)));

    [Theory]
    [InlineData("11:00", "11:00")]
    [InlineData("11:00", "10:00")]
    public void End_time_must_be_after_start_time(string start, string end) =>
        Check.ErrorOn(_create.Validate(Req(from: start, to: end)), "EndTime");

    [Fact]
    public void The_section_id_must_be_positive() =>
        Check.ErrorOn(_create.Validate(Req(sectionId: 0)), "SectionId");

    [Fact]
    public void The_date_is_required() =>
        Check.ErrorOn(_create.Validate(Req(date: default(DateOnly))), "Date");

    [Fact]
    public void A_given_room_id_must_be_positive() =>
        Check.ErrorOn(_create.Validate(Req(roomId: 0)), "RoomId");

    [Fact]
    public void Notes_are_limited_to_500_characters() =>
        Check.ErrorOn(_create.Validate(Req(notes: new string('x', 501))), "Notes");

    [Fact]
    public void Postponing_needs_a_valid_new_time_range()
    {
        var ok = new PostponeClassSessionRequest(new DateOnly(2026, 11, 4), new TimeOnly(9, 0), new TimeOnly(11, 0), "test");
        var bad = new PostponeClassSessionRequest(new DateOnly(2026, 11, 4), new TimeOnly(11, 0), new TimeOnly(9, 0), "test");

        Check.Valid(_postpone.Validate(ok));
        Check.ErrorOn(_postpone.Validate(bad), "NewEndTime");
    }
}