using EducationalCenter.Application.Features.OnlinePayments;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EducationalCenter.Infrastructure.Persistence;

internal sealed class PaymentIntentStore(AppDbContext db) : IPaymentIntentStore
{
    public async Task AddAsync(PaymentIntent intent, CancellationToken ct = default)
    {
        db.PaymentIntents.Add(intent);
        await db.SaveChangesAsync(ct);
    }

    public async Task<PaymentIntent?> FindByReferenceAsync(string reference, CancellationToken ct = default) =>
        await db.PaymentIntents.AsNoTracking().FirstOrDefaultAsync(x => x.Reference == reference, ct);

    public async Task<IReadOnlyList<PaymentIntent>> ListByUserAsync(int userId, CancellationToken ct = default) =>
        await db.PaymentIntents.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.Id)
            .Take(50)
            .ToListAsync(ct);

    public async Task<bool> TryMarkProcessingAsync(int id, CancellationToken ct = default) =>
        await db.PaymentIntents
            .Where(x => x.Id == id && x.Status == PaymentIntentStatus.Pending)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, PaymentIntentStatus.Processing), ct) == 1;

    public async Task MarkSucceededAsync(int id, int paymentId, DateTime at, CancellationToken ct = default) =>
        await db.PaymentIntents
            .Where(x => x.Id == id && x.Status == PaymentIntentStatus.Processing)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, PaymentIntentStatus.Succeeded)
                .SetProperty(x => x.PaymentId, (int?)paymentId)
                .SetProperty(x => x.CompletedAt, (DateTime?)at), ct);

    public async Task MarkFailedAsync(int id, string reason, CancellationToken ct = default)
    {
        var text = reason.Length > 500 ? reason[..500] : reason;
        await db.PaymentIntents
            .Where(x => x.Id == id && x.Status == PaymentIntentStatus.Processing)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, PaymentIntentStatus.Failed)
                .SetProperty(x => x.FailureReason, text), ct);
    }

    public async Task MarkExpiredAsync(int id, CancellationToken ct = default) =>
        await db.PaymentIntents
            .Where(x => x.Id == id && x.Status == PaymentIntentStatus.Pending)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, PaymentIntentStatus.Expired), ct);
}