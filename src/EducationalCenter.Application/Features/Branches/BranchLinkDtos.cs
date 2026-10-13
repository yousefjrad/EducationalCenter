namespace EducationalCenter.Application.Features.Branches;

public sealed record BranchCourseDto(int CourseId, string Code, string Name, decimal DefaultPrice, bool CourseIsActive, bool IsActive);

public sealed record BranchTrainerDto(int TrainerId, string FullName, string? Specialty, bool IsActive);