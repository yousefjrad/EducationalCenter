using EducationalCenter.Domain.Enums;

namespace EducationalCenter.Application.Features.Attendances;

public sealed record AttendanceMarkDto(int EnrollmentId, bool IsPresent);

/// <summary>Attendance of one session, entered by the receptionist from the trainer's paper sheet.</summary>
public sealed record RecordAttendanceRequest(IReadOnlyList<AttendanceMarkDto> Marks);

/// <param name="IsPresent">Null means not recorded yet.</param>
public sealed record AttendanceRowDto(int EnrollmentId, int StudentId, string StudentName, bool? IsPresent);

public sealed record SessionAttendanceDto(
    int SessionId,
    string SectionName,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    ClassSessionStatus Status,
    IReadOnlyList<AttendanceRowDto> Rows);

public sealed record EnrollmentAttendanceItemDto(int SessionId, DateOnly Date, TimeOnly StartTime, bool IsPresent);

public sealed record EnrollmentAttendanceDto(
    int EnrollmentId,
    string StudentName,
    string SectionName,
    int SessionsRecorded,
    int Present,
    int Absent,
    double AttendancePercentage,
    IReadOnlyList<EnrollmentAttendanceItemDto> Items);
