namespace EducationalCenter.Domain.Enums;

public enum PayrollStatus
{
    Due = 1,
    Paid = 2,
    // A wrong payroll can be cancelled only while it is still Due. It is never deleted.
    Cancelled = 3
}
