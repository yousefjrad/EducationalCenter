using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Domain.Common;
using EducationalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EducationalCenter.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Fills CreatedAt/UpdatedAt, turns deletes into soft deletes, and enforces the rules that
/// financial records are never deleted and the audit log is append-only.
/// </summary>
public sealed class AuditableEntitiesInterceptor(IClock clock) : SaveChangesInterceptor
{
    /// <summary>Financial data: cancelled by changing its status, never removed.</summary>
    private static readonly HashSet<Type> NeverDeleted =
    [
        typeof(Payment), typeof(PaymentPlan), typeof(Installment),
        typeof(Receipt), typeof(Expense), typeof(TrainerPayroll)
    ];

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext? context)
    {
        if (context is null)
            return;

        var now = clock.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries().ToList())
        {
            if (entry.Entity is AuditLog && entry.State is EntityState.Modified or EntityState.Deleted)
                throw new InvalidOperationException("The audit log is append-only and cannot be changed or deleted.");

            if (entry.Entity is not BaseEntity entity)
                continue;

            switch (entry.State)
            {
                case EntityState.Added:
                    entity.CreatedAt = now;
                    break;

                case EntityState.Modified:
                    entity.UpdatedAt = now;
                    break;

                case EntityState.Deleted:
                    if (NeverDeleted.Contains(entry.Entity.GetType()))
                        throw new InvalidOperationException(
                            $"{entry.Entity.GetType().Name} is financial data and is never deleted. Change its status instead.");

                    entry.State = EntityState.Modified;
                    entity.IsDeleted = true;
                    entity.UpdatedAt = now;
                    break;
            }
        }
    }
}
