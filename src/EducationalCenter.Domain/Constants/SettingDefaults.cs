namespace EducationalCenter.Domain.Constants;

/// <summary>The settings that exist, with their default values (seeded by Infrastructure).</summary>
public static class SettingDefaults
{
    public static readonly IReadOnlyList<(string Key, string Value, string Description)> All =
    [
        (SettingKeys.PassingScore, "60", "Passing score as a percentage of the maximum score"),
        (SettingKeys.EnrollmentHoldHours, "24", "Hours a temporary seat hold lasts before it expires"),
        (SettingKeys.CenterName, "Educational Center", "Center name shown on receipts")
    ];
}
