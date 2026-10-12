namespace EducationalCenter.Domain.Entities;

/// <summary>A trainer may teach at several branches.</summary>
public class TrainerBranch
{
    public int TrainerId { get; set; }
    public Trainer Trainer { get; set; } = null!;
    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;
}