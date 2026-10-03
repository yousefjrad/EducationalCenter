using EducationalCenter.Domain.Enums;

namespace EducationalCenter.Application.Features.ClassSessions;

public sealed record ClassSessionDto(
    int Id,
    int SectionId,
    string SectionName,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int RoomId,
    string RoomName,
    int TrainerId,
    string TrainerName,
    ClassSessionStatus Status,
    string? Notes);

/// <summary>Manual extra session. Room and trainer default to the section's.</summary>
public sealed record CreateClassSessionRequest(
    int SectionId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int? RoomId,
    int? TrainerId,
    string? Notes);

/// <summary>Reschedule / change room or trainer of a Scheduled session.</summary>
public sealed record UpdateClassSessionRequest(
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int RoomId,
    int TrainerId,
    string? Notes);

/// <summary>Marks the session Postponed and creates its replacement at the new date/time.</summary>
public sealed record PostponeClassSessionRequest(
    DateOnly NewDate,
    TimeOnly NewStartTime,
    TimeOnly NewEndTime,
    string? Reason);

public sealed record CancelClassSessionRequest(string? Reason);

public sealed record ClassSessionListQuery(
    int? SectionId = null,
    int? RoomId = null,
    int? TrainerId = null,
    DateOnly? From = null,
    DateOnly? To = null,
    ClassSessionStatus? Status = null,
    int Page = 1,
    int PageSize = 50);
