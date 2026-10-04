using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Features.Settings;

public static class SettingMappings
{
    public static SettingDto ToDto(this Setting s) => new(s.Key, s.Value, s.Description, s.UpdatedAt);
}
