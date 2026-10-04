using EducationalCenter.Domain.Common;

namespace EducationalCenter.Domain.Entities;

public class Student : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<Enrollment> Enrollments { get; set; } = [];
    public ICollection<WaitingListEntry> WaitingListEntries { get; set; } = [];
}
