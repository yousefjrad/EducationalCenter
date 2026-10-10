using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Features.Enrollments;
using EducationalCenter.Domain.Enums;
using EducationalCenter.Domain.Exceptions;
using FluentValidation;

namespace EducationalCenter.Application.Features.StudentPortal;

public sealed record BookSeatRequest(int SectionId);

public sealed class BookSeatRequestValidator : AbstractValidator<BookSeatRequest>
{
    public BookSeatRequestValidator()
    {
        RuleFor(x => x.SectionId).GreaterThan(0);
    }
}

public interface IStudentBookingService
{
    /// <summary>A temporary seat hold (Pending) for the signed-in student. The center confirms it when the student pays.</summary>
    Task<EnrollmentDto> HoldSeatAsync(int sectionId, CancellationToken ct = default);

    /// <summary>Cancels the student's own pending hold. Confirmed enrollments are cancelled by the center only.</summary>
    Task CancelHoldAsync(int enrollmentId, CancellationToken ct = default);
}

public sealed class StudentBookingService(
    IStudentPortalReader reader,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    IClock clock,
    IEnrollmentService enrollments) : IStudentBookingService
{
    /// <summary>Stops one student from blocking many seats with holds.</summary>
    public const int MaxActiveHolds = 3;

    public async Task<EnrollmentDto> HoldSeatAsync(int sectionId, CancellationToken ct = default)
    {
        var studentId = await StudentIdAsync(ct);

        var (pending, _) = await uow.Enrollments.SearchAsync(studentId, null, EnrollmentStatus.Pending, 1, 50, ct);
        var now = clock.UtcNow;
        if (pending.Count(e => e.HoldExpiresAt > now) >= MaxActiveHolds)
            throw new ConflictException(
                $"You already have {MaxActiveHolds} pending seat holds. Cancel one or contact the center.");

        return await enrollments.CreateAsync(new CreateEnrollmentRequest(studentId, sectionId, AsHold: true), ct);
    }

    public async Task CancelHoldAsync(int enrollmentId, CancellationToken ct = default)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException("Sign in is required.");

        var enrollment = await uow.Enrollments.GetWithDetailsAsync(enrollmentId, ct);
        if (enrollment is null || enrollment.Student.UserId != userId)
            throw new NotFoundException("Enrollment", enrollmentId);

        if (enrollment.Status != EnrollmentStatus.Pending)
            throw new ConflictException("Only a pending seat hold can be cancelled online. Please contact the center.");

        await enrollments.CancelAsync(enrollmentId, ct);
    }

    private async Task<int> StudentIdAsync(CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException("Sign in is required.");
        return await reader.FindStudentIdAsync(userId, ct)
            ?? throw new NotFoundException("No student profile is linked to this account.");
    }
}