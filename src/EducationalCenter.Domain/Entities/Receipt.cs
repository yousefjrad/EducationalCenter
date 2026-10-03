using EducationalCenter.Domain.Common;

namespace EducationalCenter.Domain.Entities;

public class Receipt : BaseEntity
{
    public int PaymentId { get; set; }
    public Payment Payment { get; set; } = null!;

    /// <summary>Unique sequential number.</summary>
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public int IssuedByUserId { get; set; }
    public User IssuedByUser { get; set; } = null!;
}
