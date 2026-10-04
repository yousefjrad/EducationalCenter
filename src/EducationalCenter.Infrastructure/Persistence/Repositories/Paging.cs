using Microsoft.EntityFrameworkCore;

namespace EducationalCenter.Infrastructure.Persistence.Repositories;

internal static class Paging
{
    /// <summary>The query must already be ordered. Counts first, then reads only the requested page.</summary>
    public static async Task<(IReadOnlyList<T> Items, int TotalCount)> ToPageAsync<T>(
        this IQueryable<T> query, int page, int pageSize, CancellationToken ct)
    {
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }
}
