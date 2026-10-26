using EducationalCenter.Application.Common.Exceptions;
using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Domain.Exceptions;

namespace EducationalCenter.Application.Features.Reports;

public sealed class BranchComparisonService(IUnitOfWork uow, IClock clock) : IBranchComparisonService
{
    public async Task<BranchComparisonReportDto> GetAsync(BranchComparisonQuery query, CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        var today = clock.Today;
        var from = query.From ?? new DateOnly(today.Year, today.Month, 1);
        var to = query.To ?? today;

        if (to < from)
            throw new RequestValidationException("to", "'To' must be on or after 'From'.");

        var rows = await uow.Reports.GetBranchComparisonAsync(from, to, now, today, ct);

        var totals = new BranchComparisonTotals(
            rows.Sum(r => r.RevenueSyp),
            rows.Sum(r => r.ExpensesSyp),
            rows.Sum(r => r.PayrollSyp),
            rows.Sum(r => r.NetProfitSyp),
            rows.Sum(r => r.StudentCount),
            rows.Sum(r => r.NewEnrollments),
            rows.Sum(r => r.OverdueSyp),
            rows.Sum(r => r.OverdueInstallments));

        return new BranchComparisonReportDto(from, to, rows, totals);
    }
}