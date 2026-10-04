using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Enums;
using EducationalCenter.Domain.Exceptions;

namespace EducationalCenter.Application.Features.Attendances;

public sealed class AttendanceService(IUnitOfWork uow, IClock clock, ICurrentUser currentUser) : IAttendanceService
{
    public async Task<SessionAttendanceDto> GetSessionAttendanceAsync(int sessionId, CancellationToken ct = default)
    {
        var session = await uow.ClassSessions.GetWithDetailsAsync(sessionId, ct)
            ?? throw new NotFoundException(nameof(ClassSession), sessionId);

        var roster = await uow.Enrollments.GetEnrolledBySectionAsync(session.SectionId, ct);
        var records = (await uow.Attendances.GetBySessionAsync(sessionId, ct)).ToDictionary(a => a.EnrollmentId);

        return BuildSessionDto(session, roster, records);
    }

    public Task<SessionAttendanceDto> RecordAsync(int sessionId, RecordAttendanceRequest request, CancellationToken ct = default)
    {
        return uow.ExecuteInTransactionAsync(async () =>
        {
            var userId = currentUser.RequireUserId();

            var session = await uow.ClassSessions.GetWithDetailsAsync(sessionId, ct)
                ?? throw new NotFoundException(nameof(ClassSession), sessionId);

            if (session.Status is not (ClassSessionStatus.Scheduled or ClassSessionStatus.Held))
                throw new ConflictException("Attendance can only be recorded for scheduled or held sessions.");

            // One day of tolerance so local time zones ahead of UTC are not blocked.
            var latestAllowed = DateOnly.FromDateTime(clock.UtcNow).AddDays(1);
            if (session.Date > latestAllowed)
                throw new ConflictException("Attendance cannot be recorded for a future session.");

            var roster = await uow.Enrollments.GetEnrolledBySectionAsync(session.SectionId, ct);
            var rosterIds = roster.Select(e => e.Id).ToHashSet();

            var unknown = request.Marks.Where(m => !rosterIds.Contains(m.EnrollmentId)).Select(m => m.EnrollmentId).ToList();
            if (unknown.Count > 0)
                throw new ConflictException(
                    $"These enrollments are not confirmed in this session's section: {string.Join(", ", unknown)}.");

            var records = (await uow.Attendances.GetBySessionAsync(sessionId, ct)).ToDictionary(a => a.EnrollmentId);

            foreach (var mark in request.Marks)
            {
                if (records.TryGetValue(mark.EnrollmentId, out var current))
                {
                    if (current.IsPresent != mark.IsPresent)
                    {
                        current.IsPresent = mark.IsPresent;
                        current.RecordedByUserId = userId;
                        uow.Attendances.Update(current);
                    }
                }
                else
                {
                    var created = new Attendance
                    {
                        ClassSessionId = session.Id,
                        EnrollmentId = mark.EnrollmentId,
                        IsPresent = mark.IsPresent,
                        RecordedByUserId = userId
                    };
                    await uow.Attendances.AddAsync(created, ct);
                    records[mark.EnrollmentId] = created;
                }
            }

            if (session.Status == ClassSessionStatus.Scheduled)
            {
                session.Status = ClassSessionStatus.Held;
                uow.ClassSessions.Update(session);
            }

            await uow.SaveChangesAsync(ct);
            return BuildSessionDto(session, roster, records);
        }, ct);
    }

    public async Task<EnrollmentAttendanceDto> GetEnrollmentSummaryAsync(int enrollmentId, CancellationToken ct = default)
    {
        var enrollment = await uow.Enrollments.GetWithDetailsAsync(enrollmentId, ct)
            ?? throw new NotFoundException(nameof(Enrollment), enrollmentId);

        var records = await uow.Attendances.GetByEnrollmentAsync(enrollmentId, ct);

        var present = records.Count(r => r.IsPresent);
        var total = records.Count;
        var percentage = total == 0 ? 0d : Math.Round(present * 100d / total, 1);

        var items = records
            .Select(r => new EnrollmentAttendanceItemDto(r.ClassSessionId, r.ClassSession.Date, r.ClassSession.StartTime, r.IsPresent))
            .ToList();

        return new EnrollmentAttendanceDto(
            enrollment.Id,
            enrollment.Student.FullName,
            enrollment.Section.Name,
            total,
            present,
            total - present,
            percentage,
            items);
    }

    private static SessionAttendanceDto BuildSessionDto(
        ClassSession session, IEnumerable<Enrollment> roster, IReadOnlyDictionary<int, Attendance> records)
    {
        var rows = roster
            .OrderBy(e => e.Student.FullName)
            .Select(e => new AttendanceRowDto(
                e.Id,
                e.StudentId,
                e.Student.FullName,
                records.TryGetValue(e.Id, out var record) ? record.IsPresent : (bool?)null))
            .ToList();

        return new SessionAttendanceDto(
            session.Id, session.Section.Name, session.Date, session.StartTime, session.EndTime, session.Status, rows);
    }
}
