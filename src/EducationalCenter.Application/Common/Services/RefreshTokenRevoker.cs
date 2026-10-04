using EducationalCenter.Application.Common.Interfaces;

namespace EducationalCenter.Application.Common.Services;

internal static class RefreshTokenRevoker
{
    /// <summary>Ends every active session of the user. The caller saves.</summary>
    public static async Task RevokeAllAsync(IUnitOfWork uow, int userId, DateTime utcNow, CancellationToken ct)
    {
        var active = await uow.RefreshTokens.GetActiveByUserAsync(userId, utcNow, ct);
        foreach (var token in active)
            token.RevokedAt = utcNow;
    }
}
