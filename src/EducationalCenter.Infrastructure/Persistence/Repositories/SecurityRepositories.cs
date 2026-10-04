using EducationalCenter.Application.Common.Interfaces.Repositories;
using EducationalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EducationalCenter.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(AppDbContext context) : Repository<User>(context), IUserRepository
{
    public Task<User?> GetWithRoleAsync(int id, CancellationToken ct = default) =>
        Set.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        Set.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<bool> EmailExistsAsync(string email, int? excludeId, CancellationToken ct = default) =>
        Set.AnyAsync(u => u.Email == email && (excludeId == null || u.Id != excludeId), ct);

    public Task<int> CountActiveInRoleAsync(int roleId, CancellationToken ct = default) =>
        Set.CountAsync(u => u.RoleId == roleId && u.IsActive, ct);

    public async Task<IReadOnlyList<string>> GetPermissionNamesAsync(int userId, CancellationToken ct = default) =>
        await Set.Where(u => u.Id == userId)
            .SelectMany(u => u.Role.RolePermissions)
            .Select(rp => rp.Permission.Name)
            .OrderBy(n => n)
            .ToListAsync(ct);

    public Task<(IReadOnlyList<User> Items, int TotalCount)> SearchAsync(
        string? search, int? roleId, bool? isActive, int page, int pageSize, CancellationToken ct = default)
    {
        var query = Set.AsNoTracking().Include(u => u.Role).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u => u.FullName.Contains(search) || u.Email.Contains(search));
        if (roleId.HasValue)
            query = query.Where(u => u.RoleId == roleId.Value);
        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        return query.OrderBy(u => u.FullName).ThenBy(u => u.Id).ToPageAsync(page, pageSize, ct);
    }
}

internal sealed class RoleRepository(AppDbContext context) : Repository<Role>(context), IRoleRepository
{
    public Task<Role?> GetWithPermissionsAsync(int id, CancellationToken ct = default) =>
        Set.Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<Role>> ListWithPermissionsAsync(CancellationToken ct = default) =>
        await Set.AsNoTracking()
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .OrderBy(r => r.Name)
            .ToListAsync(ct);

    public Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken ct = default) =>
        Set.AnyAsync(r => r.Name == name && (excludeId == null || r.Id != excludeId), ct);

    public Task<bool> HasUsersAsync(int roleId, CancellationToken ct = default) =>
        Db.Users.AnyAsync(u => u.RoleId == roleId, ct);

    public void RemoveRolePermissions(IEnumerable<RolePermission> items) => Db.RolePermissions.RemoveRange(items);
}

internal sealed class PermissionRepository(AppDbContext db) : IPermissionRepository
{
    public async Task<IReadOnlyList<Permission>> GetByNamesAsync(IReadOnlyCollection<string> names, CancellationToken ct = default)
    {
        var wanted = names.ToList();
        return await db.Permissions.Where(p => wanted.Contains(p.Name)).ToListAsync(ct);
    }
}

internal sealed class SettingRepository(AppDbContext db) : ISettingRepository
{
    public Task<Setting?> GetByKeyAsync(string key, CancellationToken ct = default) =>
        db.Settings.FirstOrDefaultAsync(s => s.Key == key, ct);

    public async Task<IReadOnlyList<Setting>> ListAsync(CancellationToken ct = default) =>
        await db.Settings.AsNoTracking().OrderBy(s => s.Key).ToListAsync(ct);
}

internal sealed class RefreshTokenRepository(AppDbContext db) : IRefreshTokenRepository
{
    public async Task AddAsync(RefreshToken token, CancellationToken ct = default)
    {
        await db.RefreshTokens.AddAsync(token, ct);
    }

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default) =>
        db.RefreshTokens
            .Include(t => t.User).ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task<IReadOnlyList<RefreshToken>> GetActiveByUserAsync(int userId, DateTime utcNow, CancellationToken ct = default) =>
        await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > utcNow)
            .ToListAsync(ct);
}

internal sealed class AuditLogRepository(AppDbContext db) : IAuditLogRepository
{
    public async Task AddAsync(AuditLog entry, CancellationToken ct = default)
    {
        await db.AuditLogs.AddAsync(entry, ct);
    }

    public Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> SearchAsync(
        int? userId, string? entityName, int? entityId, string? action, DateOnly? from, DateOnly? to,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.AuditLogs.AsNoTracking().Include(a => a.User).AsQueryable();

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);
        if (!string.IsNullOrWhiteSpace(entityName))
            query = query.Where(a => a.EntityName == entityName);
        if (entityId.HasValue)
            query = query.Where(a => a.EntityId == entityId.Value);
        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action == action);
        if (from.HasValue)
        {
            var start = DateRanges.StartOfDay(from.Value);
            query = query.Where(a => a.CreatedAt >= start);
        }
        if (to.HasValue)
        {
            var end = DateRanges.StartOfNextDay(to.Value);
            query = query.Where(a => a.CreatedAt < end);
        }

        return query.OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id).ToPageAsync(page, pageSize, ct);
    }
}
