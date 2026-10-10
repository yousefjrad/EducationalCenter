using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Features.Certificates;
using EducationalCenter.Application.Features.Receipts;
using EducationalCenter.Domain.Enums;
using EducationalCenter.Domain.Exceptions;

namespace EducationalCenter.Application.Features.StudentPortal;

public sealed record MyGradeDto(decimal Score, decimal MaxScore, decimal Percentage, bool IsPassed);

public sealed record MyEnrollmentDto(
    int EnrollmentId,
    string CourseName,
    string SectionName,
    SectionStatus SectionStatus,
    DateOnly StartDate,
    DateOnly EndDate,
    EnrollmentStatus Status,
    DateTime? HoldExpiresAt,
    int SessionsPresent,
    int SessionsAbsent,
    MyGradeDto? Grade,
    int? CertificateId,
    decimal? TotalAmount,
    decimal PaidAmountInSyp,
    decimal? RemainingInSyp);

public sealed record MyAttendanceDto(
    DateOnly Date, TimeOnly StartTime, TimeOnly EndTime, ClassSessionStatus SessionStatus, bool IsPresent);

public sealed record MyPaymentDto(
    DateTime PaidAt, Currency Currency, decimal AmountPaid, decimal AmountInSyp, int? ReceiptId, string? ReceiptNumber);

public sealed record MyInstallmentDto(
    int Number, decimal Amount, DateOnly DueDate, decimal PaidAmountInSyp, decimal RemainingInSyp,
    IReadOnlyList<MyPaymentDto> Payments);

public sealed record MyPaymentsDto(
    int EnrollmentId, decimal? TotalAmount, decimal PaidAmountInSyp, decimal? RemainingInSyp,
    IReadOnlyList<MyInstallmentDto> Installments);

/// <summary>Read-only queries for the student portal. Implemented in Infrastructure; every query is scoped to one student.</summary>
public interface IStudentPortalReader
{
    Task<int?> FindStudentIdAsync(int userId, CancellationToken ct = default);
    Task<IReadOnlyList<MyEnrollmentDto>> GetEnrollmentsAsync(int studentId, CancellationToken ct = default);

    /// <returns>Null when the enrollment does not belong to the student.</returns>
    Task<IReadOnlyList<MyAttendanceDto>?> GetAttendanceAsync(int studentId, int enrollmentId, CancellationToken ct = default);

    /// <returns>Null when the enrollment does not belong to the student.</returns>
    Task<MyPaymentsDto?> GetPaymentsAsync(int studentId, int enrollmentId, CancellationToken ct = default);

    Task<bool> OwnsCertificateAsync(int studentId, int certificateId, CancellationToken ct = default);
    Task<bool> OwnsReceiptAsync(int studentId, int receiptId, CancellationToken ct = default);
}

public interface IStudentPortalService
{
    Task<IReadOnlyList<MyEnrollmentDto>> GetEnrollmentsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MyAttendanceDto>> GetAttendanceAsync(int enrollmentId, CancellationToken ct = default);
    Task<MyPaymentsDto> GetPaymentsAsync(int enrollmentId, CancellationToken ct = default);
    Task<CertificateFileDto> GetCertificatePdfAsync(int certificateId, string language, CancellationToken ct = default);
    Task<ReceiptFileDto> GetReceiptPdfAsync(int receiptId, string language, CancellationToken ct = default);
}

public sealed class StudentPortalService(
    IStudentPortalReader reader,
    ICurrentUser currentUser,
    ICertificateService certificates,
    IReceiptService receipts) : IStudentPortalService
{
    public async Task<IReadOnlyList<MyEnrollmentDto>> GetEnrollmentsAsync(CancellationToken ct = default) =>
        await reader.GetEnrollmentsAsync(await StudentIdAsync(ct), ct);

    public async Task<IReadOnlyList<MyAttendanceDto>> GetAttendanceAsync(int enrollmentId, CancellationToken ct = default) =>
        await reader.GetAttendanceAsync(await StudentIdAsync(ct), enrollmentId, ct)
            ?? throw new NotFoundException("Enrollment", enrollmentId);

    public async Task<MyPaymentsDto> GetPaymentsAsync(int enrollmentId, CancellationToken ct = default) =>
        await reader.GetPaymentsAsync(await StudentIdAsync(ct), enrollmentId, ct)
            ?? throw new NotFoundException("Enrollment", enrollmentId);

    public async Task<CertificateFileDto> GetCertificatePdfAsync(int certificateId, string language, CancellationToken ct = default)
    {
        var studentId = await StudentIdAsync(ct);
        if (!await reader.OwnsCertificateAsync(studentId, certificateId, ct))
            throw new NotFoundException("Certificate", certificateId);

        return await certificates.GetPdfAsync(certificateId, language, ct);
    }

    public async Task<ReceiptFileDto> GetReceiptPdfAsync(int receiptId, string language, CancellationToken ct = default)
    {
        var studentId = await StudentIdAsync(ct);
        if (!await reader.OwnsReceiptAsync(studentId, receiptId, ct))
            throw new NotFoundException("Receipt", receiptId);

        return await receipts.GetPdfAsync(receiptId, language, ct);
    }

    private async Task<int> StudentIdAsync(CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException("Sign in is required.");
        return await reader.FindStudentIdAsync(userId, ct)
            ?? throw new NotFoundException("No student profile is linked to this account.");
    }
}