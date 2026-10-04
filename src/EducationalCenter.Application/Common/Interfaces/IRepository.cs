using EducationalCenter.Domain.Common;

namespace EducationalCenter.Application.Common.Interfaces;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);
    void Update(T entity);

    /// <summary>
    /// Soft or hard delete is decided in Infrastructure (soft by default for important data;
    /// financial data is never deleted, only cancelled by status).
    /// </summary>
    void Remove(T entity);
}
