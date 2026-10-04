using EducationalCenter.Domain.Common;
using EducationalCenter.Domain.Enums;

namespace EducationalCenter.Domain.Entities;

public class Trainer : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Specialty { get; set; }
    public TrainerPayType PayType { get; set; }
    public decimal PayValue { get; set; }
    public Currency PayCurrency { get; set; } = Currency.Syp;
    public bool IsActive { get; set; } = true;

    public ICollection<Section> Sections { get; set; } = [];
    public ICollection<ClassSession> ClassSessions { get; set; } = [];
    public ICollection<TrainerPayroll> Payrolls { get; set; } = [];
}
