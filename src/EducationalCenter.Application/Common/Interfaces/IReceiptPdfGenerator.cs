using EducationalCenter.Application.Common.Models;

namespace EducationalCenter.Application.Common.Interfaces;

/// <summary>Implemented in Infrastructure (PDF library chosen there).</summary>
public interface IReceiptPdfGenerator
{
    byte[] Generate(ReceiptPdfModel model);
}
