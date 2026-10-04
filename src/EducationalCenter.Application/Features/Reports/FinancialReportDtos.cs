namespace EducationalCenter.Application.Features.Reports;

public sealed record MonthlyRevenueQuery(DateOnly From, DateOnly To);

public sealed record MonthlyRevenueReportDto(
    DateOnly From, DateOnly To, IReadOnlyList<MonthlyRevenueRow> Rows, int TotalPayments, decimal TotalInSyp);

public sealed record StudentBalancesQuery(int? SectionId = null, int? CourseId = null);

public sealed record StudentBalancesReportDto(IReadOnlyList<StudentBalanceRow> Rows, decimal TotalRemainingInSyp);

/// <param name="AsOf">Defaults to today.</param>
public sealed record OverdueInstallmentsQuery(DateOnly? AsOf = null, int? SectionId = null);

public sealed record OverdueInstallmentsReportDto(
    DateOnly AsOf, IReadOnlyList<OverdueInstallmentRow> Rows, decimal TotalRemainingInSyp);

public sealed record NetProfitQuery(DateOnly From, DateOnly To);

/// <summary>
/// Cash basis: revenue = valid payments received; expenses = recorded expenses;
/// trainer payroll = payrolls marked Paid, each counted in the period of its payment date.
/// </summary>
public sealed record NetProfitReportDto(
    DateOnly From,
    DateOnly To,
    decimal RevenueInSyp,
    decimal ExpensesInSyp,
    decimal TrainerPayrollInSyp,
    decimal NetProfitInSyp,
    IReadOnlyList<CategoryTotal> ExpensesByCategory);
