namespace EducationalCenter.Application.Features.Students;

/// <param name="TargetBranchId">An open branch, different from the student's current one.</param>
/// <param name="Reason">Why the student moves (kept in the audit log).</param>
public sealed record TransferStudentRequest(int TargetBranchId, string Reason);

public sealed record StudentTransferResultDto(int StudentId, string FullName, int FromBranchId, int ToBranchId);