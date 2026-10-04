using EducationalCenter.Domain.Enums;

namespace EducationalCenter.Application.Features.PaymentPlans;

/// <param name="Status">Overdue is computed here (due date passed and not fully paid); it is never stored.</param>
public sealed record InstallmentDto(
    int Id,
    int Number,
    decimal Amount,
    DateOnly DueDate,
    decimal PaidAmountInSyp,
    decimal RemainingInSyp,
    InstallmentStatus Status);

public sealed record PaymentPlanDto(
    int Id,
    int EnrollmentId,
    int StudentId,
    string StudentName,
    string SectionName,
    decimal TotalAmount,
    int InstallmentsCount,
    decimal PaidAmountInSyp,
    decimal RemainingInSyp,
    PaymentPlanStatus Status,
    IReadOnlyList<InstallmentDto> Installments);

/// <param name="InstallmentsCount">1 means full payment; up to 24 monthly installments.</param>
/// <param name="FirstDueDate">Later installments fall on the same day of each following month.</param>
public sealed record CreatePaymentPlanRequest(int EnrollmentId, int InstallmentsCount, DateOnly FirstDueDate);
