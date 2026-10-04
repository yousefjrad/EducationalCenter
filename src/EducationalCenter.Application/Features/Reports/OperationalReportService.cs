using EducationalCenter.Application.Common.Exceptions;
using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Enums;
using EducationalCenter.Domain.Exceptions;

namespace EducationalCenter.Application.Features.Reports;

public sealed class OperationalReportService(IUnitOfWork uow, IClock clock, IReportExporter exporter) : IOperationalReportService
{
    public async Task<SectionOccupancyReportDto> GetSectionOccupancyAsync(SectionOccupancyQuery query, CancellationToken ct = default)
    {
        var rows = await uow.Reports.GetSectionOccupancyAsync(clock.UtcNow, query.Status, query.CourseId, ct);

        return new SectionOccupancyReportDto(
            rows,
            rows.Sum(r => r.Capacity),
            rows.Sum(r => r.SeatsTaken),
            rows.Sum(r => r.VacantSeats),
            rows.Sum(r => r.WaitingCount));
    }

    public async Task<ScheduleReportDto> GetRoomScheduleAsync(RoomScheduleQuery query, CancellationToken ct = default)
    {
        var room = await uow.Rooms.GetByIdAsync(query.RoomId, ct)
            ?? throw new NotFoundException(nameof(Room), query.RoomId);

        var sessions = await uow.ClassSessions.GetScheduleAsync(query.RoomId, null, query.From, query.To, ct);
        return BuildSchedule(room.Name, query.From, query.To, sessions);
    }

    public async Task<ScheduleReportDto> GetTrainerScheduleAsync(TrainerScheduleQuery query, CancellationToken ct = default)
    {
        var trainer = await uow.Trainers.GetByIdAsync(query.TrainerId, ct)
            ?? throw new NotFoundException(nameof(Trainer), query.TrainerId);

        var sessions = await uow.ClassSessions.GetScheduleAsync(null, query.TrainerId, query.From, query.To, ct);
        return BuildSchedule(trainer.FullName, query.From, query.To, sessions);
    }

    public async Task<ReportFileDto> ExportAsync(OperationalReportExportRequest request, CancellationToken ct = default)
    {
        var lang = request.Language;

        var table = request.Kind switch
        {
            OperationalReportKind.SectionOccupancy => ReportTables.SectionOccupancy(
                await GetSectionOccupancyAsync(new SectionOccupancyQuery(request.Status, request.CourseId), ct), lang),

            OperationalReportKind.RoomSchedule => ReportTables.Schedule(
                await GetRoomScheduleAsync(
                    new RoomScheduleQuery(
                        Require(request.RoomId, "roomId"), Require(request.From, "from"), Require(request.To, "to")), ct),
                lang, "القاعة", "room"),

            OperationalReportKind.TrainerSchedule => ReportTables.Schedule(
                await GetTrainerScheduleAsync(
                    new TrainerScheduleQuery(
                        Require(request.TrainerId, "trainerId"), Require(request.From, "from"), Require(request.To, "to")), ct),
                lang, "المدرب", "trainer"),

            _ => throw new RequestValidationException("kind", "Unsupported report kind.")
        };

        return ReportFiles.Create(table, request.Format, request.Kind.ToString(), clock.UtcNow, exporter);
    }

    private static ScheduleReportDto BuildSchedule(string ownerName, DateOnly from, DateOnly to, IReadOnlyList<ClassSession> sessions)
    {
        var rows = sessions
            .Where(s => s.Status is ClassSessionStatus.Scheduled or ClassSessionStatus.Held)
            .OrderBy(s => s.Date).ThenBy(s => s.StartTime)
            .Select(s => new ScheduleRowDto(
                s.Id, s.Date, s.StartTime, s.EndTime,
                s.Section.Course.Name, s.Section.Name, s.Room.Name, s.Trainer.FullName, s.Status))
            .ToList();

        return new ScheduleReportDto(ownerName, from, to, rows);
    }

    private static int Require(int? value, string name) =>
        value ?? throw new RequestValidationException(name, $"'{name}' is required for this report.");

    private static DateOnly Require(DateOnly? value, string name) =>
        value ?? throw new RequestValidationException(name, $"'{name}' is required for this report.");
}
