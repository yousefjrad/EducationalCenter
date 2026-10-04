using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Domain.Constants;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Enums;
using EducationalCenter.Domain.Exceptions;

namespace EducationalCenter.Application.Features.Grades;

public sealed class GradeService(IUnitOfWork uow, ISettingsProvider settings, ICurrentUser currentUser) : IGradeService
{
    /// <summary>"PassingScore" is a percentage of the maximum score.</summary>
    private const int DefaultPassingScore = 60;

    public Task<GradeDto> RecordAsync(RecordGradeRequest request, CancellationToken ct = default)
    {
        return uow.ExecuteInTransactionAsync(async () =>
        {
            var userId = currentUser.RequireUserId();

            var enrollment = await uow.Enrollments.GetWithDetailsAsync(request.EnrollmentId, ct)
                ?? throw new NotFoundException(nameof(Enrollment), request.EnrollmentId);

            if (enrollment.Status is not (EnrollmentStatus.Confirmed or EnrollmentStatus.Completed))
                throw new ConflictException("Grades can only be recorded for confirmed or completed enrollments.");

            if (enrollment.Certificate is not null)
                throw new ConflictException("A certificate was already issued, so the grade can no longer be changed.");

            var passingScore = await settings.GetIntAsync(SettingKeys.PassingScore, DefaultPassingScore, ct);
            var percentage = request.Score * 100m / request.MaxScore;
            var isPassed = percentage >= passingScore;

            var grade = enrollment.Grade;
            if (grade is null)
            {
                grade = new Grade
                {
                    EnrollmentId = enrollment.Id,
                    Enrollment = enrollment,
                    Score = request.Score,
                    MaxScore = request.MaxScore,
                    IsPassed = isPassed,
                    RecordedByUserId = userId
                };
                await uow.Grades.AddAsync(grade, ct);
            }
            else
            {
                grade.Score = request.Score;
                grade.MaxScore = request.MaxScore;
                grade.IsPassed = isPassed;
                grade.RecordedByUserId = userId;
                uow.Grades.Update(grade);
            }

            await uow.SaveChangesAsync(ct);
            return grade.ToDto(enrollment);
        }, ct);
    }

    public async Task<GradeDto> GetByEnrollmentAsync(int enrollmentId, CancellationToken ct = default)
    {
        var grade = await uow.Grades.GetByEnrollmentAsync(enrollmentId, ct)
            ?? throw new NotFoundException("Grade of enrollment", enrollmentId);
        return grade.ToDto(grade.Enrollment);
    }

    public async Task<IReadOnlyList<GradeDto>> ListBySectionAsync(int sectionId, CancellationToken ct = default)
    {
        var grades = await uow.Grades.GetBySectionAsync(sectionId, ct);
        return grades.Select(g => g.ToDto(g.Enrollment)).ToList();
    }
}
