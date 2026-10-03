namespace EducationalCenter.Application.Common.Interfaces;

/// <summary>Read access to the key/value Settings table (implemented in Infrastructure).</summary>
public interface ISettingsProvider
{
    Task<int> GetIntAsync(string key, int defaultValue, CancellationToken ct = default);
}
