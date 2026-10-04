using EducationalCenter.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EducationalCenter.Infrastructure.Persistence;

/// <summary>
/// Sequential, gap-free receipt numbers ("RCP-000001", "RCP-000002"...).
/// The highest number is read with UPDLOCK + HOLDLOCK, which holds a lock on the Receipts table until the
/// surrounding transaction ends. A second receptionist saving a payment at the same moment therefore waits
/// for the first to commit and then gets the next number, so no two receipts share a number.
/// It must be called inside a transaction (PaymentService does); outside one the lock is released at once.
/// </summary>
internal sealed class ReceiptNumberGenerator(AppDbContext db) : IReceiptNumberGenerator
{
    private const string HighestNumberSql =
        "SELECT ISNULL(MAX(TRY_CAST(SUBSTRING([ReceiptNumber], 5, 20) AS int)), 0) AS [Value] " +
        "FROM [Receipts] WITH (UPDLOCK, HOLDLOCK)";

    public async Task<string> NextAsync(CancellationToken ct = default)
    {
        var highest = await db.Database.SqlQueryRaw<int>(HighestNumberSql).SingleAsync(ct);
        return $"RCP-{highest + 1:D6}";
    }
}
