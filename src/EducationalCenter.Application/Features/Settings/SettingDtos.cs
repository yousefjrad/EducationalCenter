namespace EducationalCenter.Application.Features.Settings;

public sealed record SettingDto(string Key, string Value, string? Description, DateTime? UpdatedAt);

/// <summary>Values are stored as text; each key has its own rule (for example PassingScore is a whole number 1-100).</summary>
public sealed record UpdateSettingRequest(string Value);
