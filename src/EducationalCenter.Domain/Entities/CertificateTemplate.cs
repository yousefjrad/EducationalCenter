using EducationalCenter.Domain.Common;

namespace EducationalCenter.Domain.Entities;

public class CertificateTemplate : BaseEntity
{
    public string CenterName { get; set; } = string.Empty;
    public string? LogoPath { get; set; }
    public string SignerName { get; set; } = string.Empty;
    public string SignerTitle { get; set; } = string.Empty;
    /// <summary>Supports variables such as {StudentName}.</summary>
    public string BodyText { get; set; } = string.Empty;
    public string Language { get; set; } = "ar";
    public bool IsDefault { get; set; }
}
