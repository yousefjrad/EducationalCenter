using EducationalCenter.Domain.Enums;

namespace EducationalCenter.Application.Common.Models;

public sealed record ReceiptPdfModel(
    string CenterName,
    string ReceiptNumber,
    DateTime IssuedAtUtc,
    string StudentName,
    string CourseName,
    string SectionName,
    int InstallmentNumber,
    Currency Currency,
    decimal AmountPaid,
    decimal? ExchangeRate,
    decimal AmountInSyp,
    decimal BalanceAfterInSyp,
    string ReceivedByName,
    bool IsVoid,
    string Language);
