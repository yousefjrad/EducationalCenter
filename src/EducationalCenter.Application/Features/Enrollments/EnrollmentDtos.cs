using EducationalCenter.Domain.Enums;

namespace EducationalCenter.Application.Features.Enrollments;

public sealed record EnrollmentDto(
    int Id,
    int StudentId,
    string StudentName,
    int SectionId,
    string SectionName,
    string CourseName,
    decimal AgreedPrice,
    EnrollmentStatus Status,
    DateTime EnrolledAt,
    DateTime? HoldExpiresAt,
    int? TransferredFromEnrollmentId);

/// <param name="AsHold">
/// False: direct enrollment by the receptionist (Confirmed).
/// True: temporary seat hold (Pending) that expires after the "EnrollmentHoldHours" setting.
/// </param>
public sealed record CreateEnrollmentRequest(int StudentId, int SectionId, bool AsHold = false);

public sealed record TransferEnrollmentRequest(int TargetSectionId);

public sealed record EnrollmentListQuery(
    int? StudentId = null,
    int? SectionId = null,
    EnrollmentStatus? Status = null,
    int Page = 1,
    int PageSize = 20);
