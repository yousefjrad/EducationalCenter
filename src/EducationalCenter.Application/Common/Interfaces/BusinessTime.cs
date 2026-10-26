namespace EducationalCenter.Application.Common.Interfaces;

/// <summary>
/// The business time zone (Syria by default). Timestamps are stored in UTC; "today", report day boundaries and
/// printed times use this zone so that e.g. a payment at 01:00 on the 1st counts in the new month.
/// Set once at startup from the "App:TimeZone" setting.
/// </summary>
public static class BusinessTime
{
    private const string DefaultZoneId = "Asia/Damascus";

    private static TimeZoneInfo zone = Resolve(DefaultZoneId);

    public static TimeZoneInfo Zone => zone;

    public static void Configure(string? zoneId) =>
        zone = Resolve(string.IsNullOrWhiteSpace(zoneId) ? DefaultZoneId : zoneId.Trim());

    private static TimeZoneInfo Resolve(string id)
    {
        if (id.Equals("UTC", StringComparison.OrdinalIgnoreCase))
            return TimeZoneInfo.Utc;

        foreach (var candidate in new[] { id, "Syria Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(candidate); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }

        // Syria is UTC+3 all year since 2022; used only if the host has no time zone data at all.
        return TimeZoneInfo.CreateCustomTimeZone("Syria", TimeSpan.FromHours(3), "Syria", "Syria");
    }

    public static DateTime Now => ToLocal(DateTime.UtcNow);

    public static DateTime ToLocal(DateTime utc) => ToLocal(utc, zone);

    public static DateOnly DateOf(DateTime utc) => DateOnly.FromDateTime(ToLocal(utc, zone));

    /// <summary>The UTC instant at which the given local calendar day starts.</summary>
    public static DateTime StartOfDayUtc(DateOnly date) => StartOfDayUtc(date, zone);

    public static DateTime ToLocal(DateTime utc, TimeZoneInfo tz) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), tz);

    public static DateOnly DateOf(DateTime utc, TimeZoneInfo tz) => DateOnly.FromDateTime(ToLocal(utc, tz));

    public static DateTime StartOfDayUtc(DateOnly date, TimeZoneInfo tz)
    {
        var local = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        if (tz.IsInvalidTime(local))
            local = local.AddHours(1); // midnight skipped by a historical DST change
        return DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeToUtc(local, tz), DateTimeKind.Utc);
    }
}