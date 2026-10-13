using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace EducationalCenter.Infrastructure.Persistence;

/// <summary>
/// Sequential receipt numbers per branch ("MN-RCP-00001", "MN-RCP-00002"... and "DM-RCP-00001" for another branch).
/// The highest number of the branch is read with UPDLOCK + HOLDLOCK, which holds a lock until the surrounding
/// transaction ends. A second receptionist saving a payment of the same branch at the same moment therefore waits
/// for the first to commit and then gets the next number, so no two receipts share a number.
/// It must be called inside a transaction (PaymentService does); outside one the lock is released at once.
/// The unique index on ReceiptNumber stays as the final guard.
/// </summary>
internal sealed class ReceiptNumberGenerator(AppDbContext db) : IReceiptNumberGenerator
{
    public async Task<string> NextAsync(int branchId, CancellationToken ct = default)
    {
        var code = await db.Branches.AsNoTracking()
                       .Where(b => b.Id == branchId)
                       .Select(b => b.Code)
                       .FirstOrDefaultAsync(ct)
                   ?? throw new NotFoundException(nameof(Branch), branchId);

        var prefix = $"{code}-RCP-";
        var like = prefix + "%";
        var start = prefix.Length + 1;

        var highest = await db.Database
            .SqlQuery<int>($"SELECT ISNULL(MAX(TRY_CAST(SUBSTRING([ReceiptNumber], {start}, 20) AS int)), 0) AS [Value] FROM [Receipts] WITH (UPDLOCK, HOLDLOCK) WHERE [ReceiptNumber] LIKE {like}")
            .SingleAsync(ct);

        return $"{prefix}{highest + 1:D5}";
    }
}