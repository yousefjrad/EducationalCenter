using EducationalCenter.Domain.Enums;

namespace EducationalCenter.Application.Features.Reports;

/// <param name="Status">When empty, sections that are Draft, OpenForEnrollment or InProgress are included.</param>
public sealed record SectionOccupancyQuery(SectionStatus? Status = null, int? CourseId = null);

public sealed record SectionOccupancyReportDto(
    IReadOnlyList<SectionOccupancyRow> Rows,
    int TotalCapacity,
    int TotalSeatsTaken,
    int TotalVacantSeats,
    int TotalWaiting);

public sealed record RoomScheduleQuery(int RoomId, DateOnly From, DateOnly To);

public sealed record TrainerScheduleQuery(int TrainerId, DateOnly From, DateOnly To);

public sealed record ScheduleRowDto(
    int SessionId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string CourseName,
    string SectionName,
    string RoomName,
    string TrainerName,
    ClassSessionStatus Status);

/// <summary>Only Scheduled and Held sessions are listed (cancelled and postponed ones do not occupy the slot).</summary>
public sealed record ScheduleReportDto(string OwnerName, DateOnly From, DateOnly To, IReadOnlyList<ScheduleRowDto> Rows);
