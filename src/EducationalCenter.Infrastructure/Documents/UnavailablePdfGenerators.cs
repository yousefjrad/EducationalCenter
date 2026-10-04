using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Common.Models;
using EducationalCenter.Domain.Exceptions;

namespace EducationalCenter.Infrastructure.Documents;

/// <summary>Stand-ins while the PDF library is parked. Everything else in the system keeps working.</summary>
internal sealed class UnavailableReceiptPdfGenerator : IReceiptPdfGenerator
{
    public byte[] Generate(ReceiptPdfModel model) =>
        throw new FeatureUnavailableException("PDF receipts are not installed in this version yet.");
}

internal sealed class UnavailableCertificatePdfGenerator : ICertificatePdfGenerator
{
    public byte[] Generate(CertificatePdfModel model) =>
        throw new FeatureUnavailableException("PDF certificates are not installed in this version yet.");
}
