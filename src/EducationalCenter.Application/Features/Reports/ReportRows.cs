using EducationalCenter.Domain.Enums;

namespace EducationalCenter.Application.Features.Reports;

public sealed record MonthlyRevenueRow(
    int Year,
    int Month,
    int PaymentsCount,
    decimal TotalInSyp,
    decimal ReceivedInSyp,
    decimal ReceivedInUsd,
    decimal UsdValueInSyp);

public sealed record StudentBalanceRow(
    int EnrollmentId,
    int StudentId,
    string StudentName,
    string PhoneNumber,
    int SectionId,
    string SectionName,
    string CourseName,
    decimal AgreedPrice,
    decimal PaidInSyp,
    decimal RemainingInSyp);

/// <remarks>The repository leaves DaysOverdue at 0; the service fills it in.</remarks>
public sealed record OverdueInstallmentRow(
    int InstallmentId,
    int EnrollmentId,
    int StudentId,
    string StudentName,
    string PhoneNumber,
    string SectionName,
    int InstallmentNumber,
    DateOnly DueDate,
    int DaysOverdue,
    decimal Amount,
    decimal PaidInSyp,
    decimal RemainingInSyp);

public sealed record CategoryTotal(string Category, decimal TotalInSyp);

public sealed record NetProfitFigures(
    decimal RevenueInSyp,
    decimal ExpensesInSyp,
    decimal TrainerPayrollInSyp,
    IReadOnlyList<CategoryTotal> ExpensesByCategory);

public sealed record SectionOccupancyRow(
    int SectionId,
    string SectionName,
    string CourseName,
    string TrainerName,
    string RoomName,
    SectionStatus Status,
    int Capacity,
    int SeatsTaken,
    int WaitingCount)
{
    public int VacantSeats => Math.Max(0, Capacity - SeatsTaken);
}
