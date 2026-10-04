using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Common.Interfaces.Repositories;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token, CancellationToken ct = default);

    /// <summary>Tracked. Loads User with Role. The lookup is by the stored hash, never the raw token.</summary>
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>Tracked. Tokens of the user that are not revoked and not expired at <paramref name="utcNow"/>.</summary>
    Task<IReadOnlyList<RefreshToken>> GetActiveByUserAsync(int userId, DateTime utcNow, CancellationToken ct = default);
}
