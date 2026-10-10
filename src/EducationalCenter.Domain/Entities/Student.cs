using EducationalCenter.Domain.Common;

namespace EducationalCenter.Domain.Entities;

public class Student : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    /// <summary>Optional sign-in account of the student (role "Student"). One account per student.</summary>
    public int? UserId { get; set; }
    public User? User { get; set; }

    public ICollection<Enrollment> Enrollments { get; set; } = [];
    public ICollection<WaitingListEntry> WaitingListEntries { get; set; } = [];
}
