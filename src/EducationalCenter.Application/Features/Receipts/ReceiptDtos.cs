using EducationalCenter.Domain.Enums;

namespace EducationalCenter.Application.Features.Receipts;

/// <param name="IsVoid">True when the payment behind the receipt was cancelled.</param>
/// <param name="BalanceAfterInSyp">The student's remaining balance right after this payment.</param>
public sealed record ReceiptDto(
    int Id,
    int PaymentId,
    string ReceiptNumber,
    DateTime IssuedAt,
    string StudentName,
    string CourseName,
    string SectionName,
    int InstallmentNumber,
    Currency Currency,
    decimal AmountPaid,
    decimal? ExchangeRate,
    decimal AmountInSyp,
    decimal BalanceAfterInSyp,
    bool IsVoid);

public sealed record ReceiptFileDto(byte[] Content, string FileName);
