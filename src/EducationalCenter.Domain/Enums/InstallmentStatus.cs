namespace EducationalCenter.Domain.Enums;

public enum InstallmentStatus
{
    Unpaid = 1,
    PartiallyPaid = 2,
    Paid = 3,
    // Overdue is computed at query time only; never persisted.
    Overdue = 4
}
