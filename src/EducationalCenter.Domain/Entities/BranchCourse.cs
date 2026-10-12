namespace EducationalCenter.Domain.Entities;

/// <summary>A course from the global catalog that a branch has been enabled to offer.</summary>
public class BranchCourse
{
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;
    public bool IsActive { get; set; } = true;
}