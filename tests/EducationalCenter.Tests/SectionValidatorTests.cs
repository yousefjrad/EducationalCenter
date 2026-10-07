using EducationalCenter.Application.Features.Sections;

namespace EducationalCenter.Tests;

public class SectionValidatorTests
{
    private readonly CreateSectionRequestValidator _validator = new();
    private static readonly DateOnly Start = new(2026, 11, 1);

    private static SectionScheduleDto Slot(DayOfWeek day, string from, string to) =>
        new(day, TimeOnly.Parse(from), TimeOnly.Parse(to));

    private static CreateSectionRequest Valid(
        DateOnly? end = null,
        decimal? price = 500_000m,
        int capacity = 15,
        int minStudents = 5,
        IReadOnlyList<SectionScheduleDto>? schedules = null) =>
        new(1, 1, 1, "A", Start, end ?? Start.AddDays(27), price, capacity, minStudents,
            schedules ?? new[] { Slot(DayOfWeek.Sunday, "09:00", "11:00"), Slot(DayOfWeek.Tuesday, "09:00", "11:00") });

    [Fact]
    public void A_complete_request_is_valid() => Check.Valid(_validator.Validate(Valid()));

    [Fact]
    public void Price_may_be_omitted_to_use_the_course_default() => Check.Valid(_validator.Validate(Valid(price: null)));

    [Fact]
    public void End_date_before_start_date_is_rejected() =>
        Check.ErrorOn(_validator.Validate(Valid(end: Start.AddDays(-1))), "EndDate");

    [Fact]
    public void A_section_cannot_span_more_than_366_days() =>
        Check.ErrorOn(_validator.Validate(Valid(end: Start.AddDays(400))), "EndDate");

    [Fact]
    public void A_section_of_exactly_366_days_is_allowed() =>
        Check.Valid(_validator.Validate(Valid(end: Start.AddDays(366))));

    [Theory]
    [InlineData(0)]
    [InlineData(1001)]
    public void Capacity_must_be_between_1_and_1000(int capacity) =>
        Check.ErrorOn(_validator.Validate(Valid(capacity: capacity)), "Capacity");

    [Fact]
    public void Min_students_cannot_exceed_capacity() =>
        Check.ErrorOn(_validator.Validate(Valid(capacity: 10, minStudents: 11)), "MinStudents");

    [Fact]
    public void A_negative_price_is_rejected() =>
        Check.ErrorOn(_validator.Validate(Valid(price: -1m)), "Price");

    [Fact]
    public void A_weekly_schedule_is_required() =>
        Check.ErrorOn(_validator.Validate(Valid(schedules: Array.Empty<SectionScheduleDto>())), "Schedules");

    [Fact]
    public void Overlapping_slots_on_the_same_day_are_rejected()
    {
        var schedules = new[] { Slot(DayOfWeek.Sunday, "09:00", "11:00"), Slot(DayOfWeek.Sunday, "10:00", "12:00") };

        Check.ErrorOn(_validator.Validate(Valid(schedules: schedules)), "Schedules");
    }

    [Fact]
    public void Back_to_back_slots_on_the_same_day_are_allowed()
    {
        var schedules = new[] { Slot(DayOfWeek.Sunday, "09:00", "11:00"), Slot(DayOfWeek.Sunday, "11:00", "13:00") };

        Check.Valid(_validator.Validate(Valid(schedules: schedules)));
    }

    [Fact]
    public void The_same_hours_on_different_days_are_allowed()
    {
        var schedules = new[] { Slot(DayOfWeek.Sunday, "09:00", "11:00"), Slot(DayOfWeek.Monday, "09:00", "11:00") };

        Check.Valid(_validator.Validate(Valid(schedules: schedules)));
    }

    [Fact]
    public void A_slot_must_end_after_it_starts()
    {
        var result = _validator.Validate(Valid(schedules: new[] { Slot(DayOfWeek.Sunday, "11:00", "09:00") }));

        Assert.Contains(result.Errors, e => e.PropertyName.StartsWith("Schedules[", StringComparison.Ordinal));
    }
}