using EducationalCenter.Application.Common.Interfaces;
using Xunit;

namespace EducationalCenter.Tests;

public sealed class BusinessTimeTests
{
    private static readonly TimeZoneInfo Syria = TimeZoneInfo.CreateCustomTimeZone("Syria", TimeSpan.FromHours(3), "Syria", "Syria");

    [Fact]
    public void Payment_at_one_am_local_on_the_first_belongs_to_the_new_month()
    {
        var utc = new DateTime(2026, 10, 31, 22, 0, 0, DateTimeKind.Utc); // 01:00 on 1 Nov in Syria
        Assert.Equal(new DateOnly(2026, 11, 1), BusinessTime.DateOf(utc, Syria));
    }

    [Fact]
    public void Local_day_starts_three_hours_before_utc_midnight()
    {
        var start = BusinessTime.StartOfDayUtc(new DateOnly(2026, 11, 1), Syria);
        Assert.Equal(new DateTime(2026, 10, 31, 21, 0, 0, DateTimeKind.Utc), start);
    }

    [Fact]
    public void Day_range_covers_exactly_24_hours()
    {
        var start = BusinessTime.StartOfDayUtc(new DateOnly(2026, 11, 1), Syria);
        var next = BusinessTime.StartOfDayUtc(new DateOnly(2026, 11, 2), Syria);
        Assert.Equal(TimeSpan.FromHours(24), next - start);
    }
}