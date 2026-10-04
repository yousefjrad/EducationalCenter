using EducationalCenter.Application.Common.Exceptions;
using EducationalCenter.Application.Common.Interfaces;

namespace EducationalCenter.Application.Features.Reports;

public sealed class FinancialReportService(IUnitOfWork uow, IClock clock, IReportExporter exporter) : IFinancialReportService
{
    public async Task<MonthlyRevenueReportDto> GetMonthlyRevenueAsync(MonthlyRevenueQuery query, CancellationToken ct = default)
    {
        var rows = await uow.Reports.GetMonthlyRevenueAsync(query.From, query.To, ct);
        return new MonthlyRevenueReportDto(
            query.From, query.To, rows, rows.Sum(r => r.PaymentsCount), rows.Sum(r => r.TotalInSyp));
    }

    public async Task<StudentBalancesReportDto> GetStudentBalancesAsync(StudentBalancesQuery query, CancellationToken ct = default)
    {
        var rows = await uow.Reports.GetStudentBalancesAsync(query.SectionId, query.CourseId, ct);
        return new StudentBalancesReportDto(rows, rows.Sum(r => r.RemainingInSyp));
    }

    public async Task<OverdueInstallmentsReportDto> GetOverdueInstallmentsAsync(OverdueInstallmentsQuery query, CancellationToken ct = default)
    {
        var asOf = query.AsOf ?? DateOnly.FromDateTime(clock.UtcNow);

        var rows = (await uow.Reports.GetOverdueInstallmentsAsync(asOf, query.SectionId, ct))
            .Select(r => r with { DaysOverdue = Math.Max(0, asOf.DayNumber - r.DueDate.DayNumber) })
            .ToList();

        return new OverdueInstallmentsReportDto(asOf, rows, rows.Sum(r => r.RemainingInSyp));
    }

    public async Task<NetProfitReportDto> GetNetProfitAsync(NetProfitQuery query, CancellationToken ct = default)
    {
        var figures = await uow.Reports.GetNetProfitFiguresAsync(query.From, query.To, ct);

        return new NetProfitReportDto(
            query.From,
            query.To,
            figures.RevenueInSyp,
            figures.ExpensesInSyp,
            figures.TrainerPayrollInSyp,
            figures.RevenueInSyp - figures.ExpensesInSyp - figures.TrainerPayrollInSyp,
            figures.ExpensesByCategory);
    }

    public async Task<ReportFileDto> ExportAsync(FinancialReportExportRequest request, CancellationToken ct = default)
    {
        var lang = request.Language;

        var table = request.Kind switch
        {
            FinancialReportKind.MonthlyRevenue => ReportTables.MonthlyRevenue(
                await GetMonthlyRevenueAsync(
                    new MonthlyRevenueQuery(Require(request.From, "from"), Require(request.To, "to")), ct), lang),

            FinancialReportKind.StudentBalances => ReportTables.StudentBalances(
                await GetStudentBalancesAsync(
                    new StudentBalancesQuery(request.SectionId, request.CourseId), ct), lang),

            FinancialReportKind.OverdueInstallments => ReportTables.OverdueInstallments(
                await GetOverdueInstallmentsAsync(
                    new OverdueInstallmentsQuery(request.AsOf, request.SectionId), ct), lang),

            FinancialReportKind.NetProfit => ReportTables.NetProfit(
                await GetNetProfitAsync(
                    new NetProfitQuery(Require(request.From, "from"), Require(request.To, "to")), ct), lang),

            _ => throw new RequestValidationException("kind", "Unsupported report kind.")
        };

        return ReportFiles.Create(table, request.Format, request.Kind.ToString(), clock.UtcNow, exporter);
    }

    private static DateOnly Require(DateOnly? value, string name) =>
        value ?? throw new RequestValidationException(name, $"'{name}' is required for this report.");
}
