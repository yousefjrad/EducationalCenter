using System.Globalization;
using System.Text.RegularExpressions;
using EducationalCenter.Application.Features.Sections;

namespace EducationalCenter.Application.Features.Imports;

internal static class ImportParsing
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private static readonly Regex SlotPattern = new(
        @"^(Sun|Mon|Tue|Wed|Thu|Fri|Sat)\s+(\d{1,2}:\d{2})\s*-\s*(\d{1,2}:\d{2})$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Dictionary<string, DayOfWeek> Days = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Sun"] = DayOfWeek.Sunday, ["Mon"] = DayOfWeek.Monday, ["Tue"] = DayOfWeek.Tuesday,
        ["Wed"] = DayOfWeek.Wednesday, ["Thu"] = DayOfWeek.Thursday, ["Fri"] = DayOfWeek.Friday,
        ["Sat"] = DayOfWeek.Saturday
    };

    /// <summary>Whole non-negative number, digits only.</summary>
    public static bool TryParseInt(string text, out int value) =>
        int.TryParse(text, NumberStyles.None, Inv, out value);

    /// <summary>Non-negative decimal with a dot as the separator and no thousands separators.</summary>
    public static bool TryParseDecimal(string text, out decimal value) =>
        decimal.TryParse(text, NumberStyles.AllowDecimalPoint, Inv, out value);

    public static bool TryParseDate(string text, out DateOnly value) =>
        DateOnly.TryParseExact(text, "yyyy-MM-dd", Inv, DateTimeStyles.None, out value);

    /// <summary>Parses "Sat 10:00-12:00; Mon 10:00-12:00" into weekly slots.</summary>
    public static bool TryParseSchedule(string text, out List<SectionScheduleDto> slots, out string error)
    {
        slots = [];
        error = string.Empty;

        var parts = text.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            error = "The schedule is empty.";
            return false;
        }

        foreach (var part in parts)
        {
            var match = SlotPattern.Match(part);
            if (!match.Success
                || !TimeOnly.TryParseExact(match.Groups[2].Value, "H:mm", Inv, DateTimeStyles.None, out var start)
                || !TimeOnly.TryParseExact(match.Groups[3].Value, "H:mm", Inv, DateTimeStyles.None, out var end))
            {
                error = $"'{part}' is not a valid slot. Use for example: Sat 10:00-12:00";
                return false;
            }

            slots.Add(new SectionScheduleDto(Days[match.Groups[1].Value], start, end));
        }

        return true;
    }
}
