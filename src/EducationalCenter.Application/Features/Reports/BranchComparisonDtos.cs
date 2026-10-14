namespace EducationalCenter.Application.Features.Reports;

/// <param name="From">Defaults to the first day of the current month.</param>
/// <param name="To">Defaults to today.</param>
public sealed record BranchComparisonQuery(DateOnly? From = null, DateOnly? To = null);

/// <summary>
/// One branch over the chosen period. Money is in SYP. Percent values are 0 to 100 (one decimal), or null when there is nothing to measure.
/// </summary>
/// <param name="AttendanceRatePercent">Present records / all attendance records of sessions dated inside the period.</param>
/// <param name="SectionFillRatePercent">Seats taken / capacity of the sections that are open or in progress now.</param>
/// <param name="CollectionRatePercent">Collected / due, over installments whose due date is inside the period.</param>
/// <param name="OverdueSyp">Remaining amount of overdue installments as of today (not limited to the period).</param>
public sealed record BranchComparisonRow(
    int BranchId,
    string BranchName,
    string BranchCode,
    bool IsActive,
    decimal RevenueSyp,
    decimal ExpensesSyp,
    decimal PayrollSyp,
    decimal NetProfitSyp,
    int StudentCount,
    int NewEnrollments,
    decimal? AttendanceRatePercent,
    decimal? SectionFillRatePercent,
    decimal? CollectionRatePercent,
    decimal OverdueSyp,
    int OverdueInstallments);

public sealed record BranchComparisonTotals(
    decimal RevenueSyp,
    decimal ExpensesSyp,
    decimal PayrollSyp,
    decimal NetProfitSyp,
    int StudentCount,
    int NewEnrollments,
    decimal OverdueSyp,
    int OverdueInstallments);

public sealed record BranchComparisonReportDto(
    DateOnly From,
    DateOnly To,
    IReadOnlyList<BranchComparisonRow> Branches,
    BranchComparisonTotals Totals);