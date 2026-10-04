namespace EducationalCenter.Application.Features.Settings;

public interface ISettingService
{
    Task<IReadOnlyList<SettingDto>> ListAsync(CancellationToken ct = default);
    Task<SettingDto> GetByKeyAsync(string key, CancellationToken ct = default);

    /// <summary>Only existing keys can be changed; new keys are defined in code and seeded.</summary>
    Task<SettingDto> UpdateAsync(string key, UpdateSettingRequest request, CancellationToken ct = default);
}
