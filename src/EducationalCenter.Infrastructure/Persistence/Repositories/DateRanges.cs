namespace EducationalCenter.Infrastructure.Persistence.Repositories;

internal static class DateRanges
{
    /// <summary>First instant of the day (UTC).</summary>
    public static DateTime StartOfDay(DateOnly date) => date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

    /// <summary>First instant of the next day, so "to" dates are inclusive when used with "&lt;".</summary>
    public static DateTime StartOfNextDay(DateOnly date) => date.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
}
