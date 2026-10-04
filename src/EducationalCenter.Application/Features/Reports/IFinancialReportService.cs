namespace EducationalCenter.Application.Features.Reports;

/// <summary>Admin only (API policy "Reports.Financial").</summary>
public interface IFinancialReportService
{
    Task<MonthlyRevenueReportDto> GetMonthlyRevenueAsync(MonthlyRevenueQuery query, CancellationToken ct = default);
    Task<StudentBalancesReportDto> GetStudentBalancesAsync(StudentBalancesQuery query, CancellationToken ct = default);
    Task<OverdueInstallmentsReportDto> GetOverdueInstallmentsAsync(OverdueInstallmentsQuery query, CancellationToken ct = default);
    Task<NetProfitReportDto> GetNetProfitAsync(NetProfitQuery query, CancellationToken ct = default);

    /// <summary>The same data as the JSON reports, as an Excel or PDF file in Arabic or English.</summary>
    Task<ReportFileDto> ExportAsync(FinancialReportExportRequest request, CancellationToken ct = default);
}
