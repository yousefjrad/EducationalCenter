using EducationalCenter.Domain.Enums;

namespace EducationalCenter.Application.Features.WaitingList;

public sealed record WaitingListEntryDto(
    int Id,
    int StudentId,
    string StudentName,
    string StudentPhone,
    int SectionId,
    string SectionName,
    int Position,
    WaitingListStatus Status,
    DateTime AddedAt,
    int? PromotedEnrollmentId);

public sealed record AddToWaitingListRequest(int StudentId, int SectionId);

/// <param name="AsHold">Give the promoted student a temporary hold instead of a confirmed enrollment.</param>
public sealed record PromoteWaitingListEntryRequest(bool AsHold = false);

public sealed record WaitingListQuery(
    int? SectionId = null,
    int? StudentId = null,
    WaitingListStatus? Status = null,
    int Page = 1,
    int PageSize = 20);
