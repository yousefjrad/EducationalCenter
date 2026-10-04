namespace EducationalCenter.Application.Features.Grades;

public sealed record GradeDto(
    int Id,
    int EnrollmentId,
    int StudentId,
    string StudentName,
    string SectionName,
    decimal Score,
    decimal MaxScore,
    decimal Percentage,
    bool IsPassed);

/// <summary>Records (or replaces) the final grade of an enrollment.</summary>
public sealed record RecordGradeRequest(int EnrollmentId, decimal Score, decimal MaxScore = 100m);
