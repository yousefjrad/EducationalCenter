namespace EducationalCenter.Application.Features.Reports;

/// <summary>Available to receptionists (API policy "Reports.Operational").</summary>
public interface IOperationalReportService
{
    Task<SectionOccupancyReportDto> GetSectionOccupancyAsync(SectionOccupancyQuery query, CancellationToken ct = default);
    Task<ScheduleReportDto> GetRoomScheduleAsync(RoomScheduleQuery query, CancellationToken ct = default);
    Task<ScheduleReportDto> GetTrainerScheduleAsync(TrainerScheduleQuery query, CancellationToken ct = default);

    Task<ReportFileDto> ExportAsync(OperationalReportExportRequest request, CancellationToken ct = default);
}
