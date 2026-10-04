using EducationalCenter.Application.Common.Exceptions;
using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Common.Models;
using EducationalCenter.Domain.Constants;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Enums;
using EducationalCenter.Domain.Exceptions;

namespace EducationalCenter.Application.Features.Receipts;

public sealed class ReceiptService(
    IUnitOfWork uow,
    ISettingsProvider settings,
    IReceiptPdfGenerator pdfGenerator) : IReceiptService
{
    private static readonly string[] Languages = ["ar", "en"];

    public async Task<ReceiptDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var receipt = await uow.Receipts.GetWithDetailsAsync(id, ct)
            ?? throw new NotFoundException(nameof(Receipt), id);
        return await BuildDtoAsync(receipt, ct);
    }

    public async Task<ReceiptDto> GetByPaymentAsync(int paymentId, CancellationToken ct = default)
    {
        var receipt = await uow.Receipts.GetByPaymentIdAsync(paymentId, ct)
            ?? throw new NotFoundException("Receipt of payment", paymentId);
        return await BuildDtoAsync(receipt, ct);
    }

    public async Task<ReceiptFileDto> GetPdfAsync(int id, string language = "ar", CancellationToken ct = default)
    {
        if (!Languages.Contains(language))
            throw new RequestValidationException("language", "'language' must be 'ar' or 'en'.");

        var receipt = await uow.Receipts.GetWithDetailsAsync(id, ct)
            ?? throw new NotFoundException(nameof(Receipt), id);

        var dto = await BuildDtoAsync(receipt, ct);
        var centerName = await settings.GetStringAsync(SettingKeys.CenterName, "Educational Center", ct);

        var model = new ReceiptPdfModel(
            centerName,
            dto.ReceiptNumber,
            dto.IssuedAt,
            dto.StudentName,
            dto.CourseName,
            dto.SectionName,
            dto.InstallmentNumber,
            dto.Currency,
            dto.AmountPaid,
            dto.ExchangeRate,
            dto.AmountInSyp,
            dto.BalanceAfterInSyp,
            receipt.Payment.ReceivedByUser.FullName,
            dto.IsVoid,
            language);

        return new ReceiptFileDto(pdfGenerator.Generate(model), $"receipt-{dto.ReceiptNumber}.pdf");
    }

    private async Task<ReceiptDto> BuildDtoAsync(Receipt receipt, CancellationToken ct)
    {
        var payment = receipt.Payment;
        var enrollment = payment.Installment.PaymentPlan.Enrollment;

        // Balance right after this payment: price minus every valid payment made up to and including it.
        var allPayments = await uow.Payments.GetByEnrollmentAsync(enrollment.Id, ct);
        var paidUpToHere = allPayments
            .Where(p => p.Status == PaymentStatus.Valid
                        && (p.PaidAt < payment.PaidAt || (p.PaidAt == payment.PaidAt && p.Id <= payment.Id)))
            .Sum(p => p.AmountInSyp);

        return new ReceiptDto(
            receipt.Id,
            payment.Id,
            receipt.ReceiptNumber,
            receipt.IssuedAt,
            enrollment.Student.FullName,
            enrollment.Section.Course.Name,
            enrollment.Section.Name,
            payment.Installment.Number,
            payment.Currency,
            payment.AmountPaid,
            payment.ExchangeRate,
            payment.AmountInSyp,
            Math.Max(0m, enrollment.AgreedPrice - paidUpToHere),
            payment.Status == PaymentStatus.Cancelled);
    }
}
