using EducationalCenter.Domain.Enums;

namespace EducationalCenter.Application.Features.Reports;

public enum ReportFormat
{
    Excel = 1,
    Pdf = 2
}

public enum FinancialReportKind
{
    MonthlyRevenue = 1,
    StudentBalances = 2,
    OverdueInstallments = 3,
    NetProfit = 4
}

public enum OperationalReportKind
{
    SectionOccupancy = 1,
    RoomSchedule = 2,
    TrainerSchedule = 3
}

/// <remarks>
/// MonthlyRevenue and NetProfit need From and To. StudentBalances uses SectionId/CourseId.
/// OverdueInstallments uses AsOf and SectionId.
/// </remarks>
public sealed record FinancialReportExportRequest(
    FinancialReportKind Kind,
    ReportFormat Format,
    DateOnly? From = null,
    DateOnly? To = null,
    DateOnly? AsOf = null,
    int? SectionId = null,
    int? CourseId = null,
    string Language = "ar");

/// <remarks>
/// RoomSchedule needs RoomId, From and To. TrainerSchedule needs TrainerId, From and To.
/// SectionOccupancy uses Status and CourseId.
/// </remarks>
public sealed record OperationalReportExportRequest(
    OperationalReportKind Kind,
    ReportFormat Format,
    DateOnly? From = null,
    DateOnly? To = null,
    int? RoomId = null,
    int? TrainerId = null,
    int? CourseId = null,
    SectionStatus? Status = null,
    string Language = "ar");

public sealed record ReportFileDto(byte[] Content, string FileName, string ContentType);
