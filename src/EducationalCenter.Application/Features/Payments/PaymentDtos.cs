using EducationalCenter.Domain.Enums;

namespace EducationalCenter.Application.Features.Payments;

public sealed record PaymentDto(
    int Id,
    int InstallmentId,
    int InstallmentNumber,
    int EnrollmentId,
    string StudentName,
    string SectionName,
    Currency Currency,
    decimal AmountPaid,
    decimal? ExchangeRate,
    decimal AmountInSyp,
    DateTime PaidAt,
    int ReceivedByUserId,
    PaymentStatus Status,
    string? CancelReason,
    int? ReceiptId,
    string? ReceiptNumber);

/// <param name="ExchangeRate">SYP per 1 USD, entered by the receptionist. Required for USD, must be empty for SYP.</param>
public sealed record RecordPaymentRequest(
    int InstallmentId,
    Currency Currency,
    decimal AmountPaid,
    decimal? ExchangeRate);

public sealed record CancelPaymentRequest(string Reason);

public sealed record PaymentListQuery(
    int? EnrollmentId = null,
    int? StudentId = null,
    DateOnly? From = null,
    DateOnly? To = null,
    PaymentStatus? Status = null,
    int Page = 1,
    int PageSize = 20);
