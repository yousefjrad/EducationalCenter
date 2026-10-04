using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace EducationalCenter.Infrastructure.Persistence.Repositories;

internal abstract class Repository<T>(AppDbContext context) : IRepository<T> where T : BaseEntity
{
    protected AppDbContext Db { get; } = context;

    protected DbSet<T> Set => Db.Set<T>();

    /// <summary>Tracked, so the caller can change it and save. Soft-deleted rows are filtered out.</summary>
    public Task<T?> GetByIdAsync(int id, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default) =>
        await Set.AsNoTracking().ToListAsync(ct);

    public async Task AddAsync(T entity, CancellationToken ct = default)
    {
        await Set.AddAsync(entity, ct);
    }

    public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
    {
        await Set.AddRangeAsync(entities, ct);
    }

    /// <summary>Tracked entities are saved automatically; only a detached one needs to be attached.</summary>
    public void Update(T entity)
    {
        if (Db.Entry(entity).State == EntityState.Detached)
            Set.Update(entity);
    }

    /// <summary>The save interceptor turns this into a soft delete (and refuses it for financial data).</summary>
    public void Remove(T entity) => Set.Remove(entity);
}
