using System.Globalization;
using EducationalCenter.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EducationalCenter.Infrastructure.Persistence;

/// <summary>
/// Reads settings straight from the database on every call. The table has a handful of rows and a unique
/// index on Key, and not caching means a changed setting applies immediately.
/// </summary>
internal sealed class DatabaseSettingsProvider(AppDbContext db) : ISettingsProvider
{
    public async Task<int> GetIntAsync(string key, int defaultValue, CancellationToken ct = default)
    {
        var value = await GetValueAsync(key, ct);

        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
            ? number
            : defaultValue;
    }

    public async Task<string> GetStringAsync(string key, string defaultValue, CancellationToken ct = default)
    {
        var value = await GetValueAsync(key, ct);
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }

    private Task<string?> GetValueAsync(string key, CancellationToken ct) =>
        db.Settings.AsNoTracking().Where(s => s.Key == key).Select(s => s.Value).FirstOrDefaultAsync(ct);
}
