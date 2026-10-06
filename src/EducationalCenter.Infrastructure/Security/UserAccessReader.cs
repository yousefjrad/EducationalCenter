using EducationalCenter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EducationalCenter.Infrastructure.Security;

/// <summary>Whether the user is active and which permissions their role holds (read from the database).</summary>
public sealed record UserAccess(bool IsActive, IReadOnlySet<string> Permissions);

public interface IUserAccessReader
{
    /// <returns>null when the user does not exist.</returns>
    Task<UserAccess?> GetAsync(int userId, CancellationToken ct = default);
}

internal sealed class UserAccessReader(AppDbContext db) : IUserAccessReader
{
    public async Task<UserAccess?> GetAsync(int userId, CancellationToken ct = default)
    {
        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.IsActive, u.RoleId })
            .FirstOrDefaultAsync(ct);

        if (user is null)
            return null;

        var names = await db.Roles.AsNoTracking()
            .Where(r => r.Id == user.RoleId)
            .SelectMany(r => r.RolePermissions)
            .Select(rp => rp.Permission.Name)
            .ToListAsync(ct);

        return new UserAccess(user.IsActive, names.ToHashSet());
    }
}