using EducationalCenter.Application.Features.Reports;

namespace EducationalCenter.Application.Features.Alerts;

/// <summary>
/// The front desk's to-do list, computed from live data each time (nothing is stored).
/// A section is null when the signed-in user is not allowed to see that kind of data.
/// </summary>
public sealed record AlertsDto(
    OverdueInstallmentsAlert? OverdueInstallments,
    PendingEnrollmentsAlert? PendingEnrollments,
    WaitingListAlert? WaitingList,
    int TotalCount);

/// <param name="Count">All overdue installments; Items holds the 20 oldest.</param>
public sealed record OverdueInstallmentsAlert(
    int Count,
    decimal TotalRemainingInSyp,
    IReadOnlyList<OverdueInstallmentRow> Items);

public sealed record PendingEnrollmentAlertItem(
    int EnrollmentId,
    int StudentId,
    string StudentName,
    string StudentPhone,
    int SectionId,
    string SectionName,
    DateTime? HoldExpiresAt,
    bool IsExpired);

/// <param name="Count">Seat holds waiting for the receptionist to confirm; Items holds the 20 closest to expiring.</param>
/// <param name="ExpiredCount">Holds whose time ran out and that have not been cancelled by the background job yet.</param>
public sealed record PendingEnrollmentsAlert(
    int Count,
    int ExpiredCount,
    IReadOnlyList<PendingEnrollmentAlertItem> Items);

/// <summary>A section that has a free seat and people waiting: the receptionist can promote the first one.</summary>
public sealed record WaitingPromotionRow(
    int SectionId,
    string SectionName,
    string CourseName,
    int VacantSeats,
    int WaitingCount,
    int FirstEntryId,
    int FirstStudentId,
    string FirstStudentName,
    string FirstStudentPhone);

public sealed record WaitingListAlert(int Count, IReadOnlyList<WaitingPromotionRow> Items);
