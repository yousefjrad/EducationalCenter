namespace EducationalCenter.Application.Features.Receipts;

public interface IReceiptService
{
    Task<ReceiptDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ReceiptDto> GetByPaymentAsync(int paymentId, CancellationToken ct = default);

    /// <param name="language">"ar" or "en".</param>
    Task<ReceiptFileDto> GetPdfAsync(int id, string language = "ar", CancellationToken ct = default);
}
