using System.Globalization;
using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Common.Models;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Enums;
using EducationalCenter.Domain.Exceptions;

namespace EducationalCenter.Application.Features.ClassSessions;

public sealed class ClassSessionService(IUnitOfWork uow) : IClassSessionService
{
    public async Task<ClassSessionDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var session = await uow.ClassSessions.GetWithDetailsAsync(id, ct)
            ?? throw new NotFoundException(nameof(ClassSession), id);
        return session.ToDto();
    }

    public async Task<PagedResult<ClassSessionDto>> ListAsync(ClassSessionListQuery query, CancellationToken ct = default)
    {
        var (items, total) = await uow.ClassSessions.SearchAsync(
            query.SectionId, query.RoomId, query.TrainerId, query.From, query.To, query.Status,
            query.Page, query.PageSize, ct);

        return new PagedResult<ClassSessionDto>(
            items.Select(s => s.ToDto()).ToList(), total, query.Page, query.PageSize);
    }

    public async Task<IReadOnlyList<ClassSessionDto>> GenerateForSectionAsync(int sectionId, CancellationToken ct = default)
    {
        var section = await uow.Sections.GetWithDetailsAsync(sectionId, ct)
            ?? throw new NotFoundException(nameof(Section), sectionId);

        if (section.Status is SectionStatus.Completed or SectionStatus.Cancelled)
            throw new ConflictException("Sessions cannot be generated for a completed or cancelled section.");

        if (await uow.Sections.HasSessionsAsync(sectionId, ct))
            throw new ConflictException("Sessions were already generated for this section.");

        var candidates = new List<ClassSession>();
        for (var date = section.StartDate; date <= section.EndDate; date = date.AddDays(1))
        {
            var weekday = date.DayOfWeek;
            foreach (var slot in section.Schedules.Where(x => x.DayOfWeek == weekday))
            {
                candidates.Add(new ClassSession
                {
                    SectionId = section.Id,
                    Section = section,
                    Date = date,
                    StartTime = slot.StartTime,
                    EndTime = slot.EndTime,
                    RoomId = section.RoomId,
                    Room = section.Room,
                    TrainerId = section.TrainerId,
                    Trainer = section.Trainer,
                    Status = ClassSessionStatus.Scheduled
                });
            }
        }

        if (candidates.Count == 0)
            throw new ConflictException("The weekly schedule produces no sessions between the start and end dates.");

        // All-or-nothing: conflicts are checked and sessions saved in one transaction.
        await uow.ExecuteInTransactionAsync(async () =>
        {
            var conflicts = new List<string>();
            foreach (var candidate in candidates)
            {
                if (await uow.ClassSessions.HasRoomConflictAsync(
                        candidate.RoomId, candidate.Date, candidate.StartTime, candidate.EndTime, null, ct))
                    conflicts.Add($"{Fmt(candidate.Date)} (room)");

                if (await uow.ClassSessions.HasTrainerConflictAsync(
                        candidate.TrainerId, candidate.Date, candidate.StartTime, candidate.EndTime, null, ct))
                    conflicts.Add($"{Fmt(candidate.Date)} (trainer)");
            }

            if (conflicts.Count > 0)
            {
                var shown = string.Join(", ", conflicts.Take(10));
                var more = conflicts.Count > 10 ? $" and {conflicts.Count - 10} more" : string.Empty;
                throw new ConflictException(
                    $"Cannot generate sessions, {conflicts.Count} conflict(s) found: {shown}{more}.");
            }

            await uow.ClassSessions.AddRangeAsync(candidates, ct);
            await uow.SaveChangesAsync(ct);
        }, ct);

        return candidates.Select(c => c.ToDto()).ToList();
    }

    public async Task<ClassSessionDto> CreateAsync(CreateClassSessionRequest request, CancellationToken ct = default)
    {
        var section = await uow.Sections.GetWithDetailsAsync(request.SectionId, ct)
            ?? throw new NotFoundException(nameof(Section), request.SectionId);

        if (section.Status is SectionStatus.Completed or SectionStatus.Cancelled)
            throw new ConflictException("Sessions cannot be added to a completed or cancelled section.");

        var room = request.RoomId is null || request.RoomId == section.RoomId
            ? section.Room
            : await GetActiveRoomAsync(request.RoomId.Value, ct);

        var trainer = request.TrainerId is null || request.TrainerId == section.TrainerId
            ? section.Trainer
            : await GetActiveTrainerAsync(request.TrainerId.Value, ct);

        await EnsureNoConflictsAsync(room.Id, trainer.Id, request.Date, request.StartTime, request.EndTime, null, ct);

        var session = new ClassSession
        {
            SectionId = section.Id,
            Section = section,
            Date = request.Date,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            RoomId = room.Id,
            Room = room,
            TrainerId = trainer.Id,
            Trainer = trainer,
            Status = ClassSessionStatus.Scheduled,
            Notes = request.Notes?.Trim()
        };

        await uow.ClassSessions.AddAsync(session, ct);
        await uow.SaveChangesAsync(ct);
        return session.ToDto();
    }

    public async Task<ClassSessionDto> UpdateAsync(int id, UpdateClassSessionRequest request, CancellationToken ct = default)
    {
        var session = await uow.ClassSessions.GetWithDetailsAsync(id, ct)
            ?? throw new NotFoundException(nameof(ClassSession), id);

        if (session.Status != ClassSessionStatus.Scheduled)
            throw new ConflictException("Only scheduled sessions can be modified.");

        var room = request.RoomId == session.RoomId ? session.Room : await GetActiveRoomAsync(request.RoomId, ct);
        var trainer = request.TrainerId == session.TrainerId
            ? session.Trainer
            : await GetActiveTrainerAsync(request.TrainerId, ct);

        await EnsureNoConflictsAsync(room.Id, trainer.Id, request.Date, request.StartTime, request.EndTime, id, ct);

        session.Date = request.Date;
        session.StartTime = request.StartTime;
        session.EndTime = request.EndTime;
        session.RoomId = room.Id;
        session.Room = room;
        session.TrainerId = trainer.Id;
        session.Trainer = trainer;
        session.Notes = request.Notes?.Trim();

        uow.ClassSessions.Update(session);
        await uow.SaveChangesAsync(ct);
        return session.ToDto();
    }

    public async Task<ClassSessionDto> PostponeAsync(int id, PostponeClassSessionRequest request, CancellationToken ct = default)
    {
        var session = await uow.ClassSessions.GetWithDetailsAsync(id, ct)
            ?? throw new NotFoundException(nameof(ClassSession), id);

        if (session.Status != ClassSessionStatus.Scheduled)
            throw new ConflictException("Only scheduled sessions can be postponed.");

        // The old session stops occupying its slot, so it is excluded from the conflict check.
        await EnsureNoConflictsAsync(
            session.RoomId, session.TrainerId, request.NewDate, request.NewStartTime, request.NewEndTime, id, ct);

        var replacement = new ClassSession
        {
            SectionId = session.SectionId,
            Section = session.Section,
            Date = request.NewDate,
            StartTime = request.NewStartTime,
            EndTime = request.NewEndTime,
            RoomId = session.RoomId,
            Room = session.Room,
            TrainerId = session.TrainerId,
            Trainer = session.Trainer,
            Status = ClassSessionStatus.Scheduled,
            Notes = $"Postponed from {Fmt(session.Date)}"
        };

        session.Status = ClassSessionStatus.Postponed;
        session.Notes = AppendNote(session.Notes, $"Postponed to {Fmt(request.NewDate)}", request.Reason);

        await uow.ExecuteInTransactionAsync(async () =>
        {
            uow.ClassSessions.Update(session);
            await uow.ClassSessions.AddAsync(replacement, ct);
            await uow.SaveChangesAsync(ct);
        }, ct);

        return replacement.ToDto();
    }

    public async Task<ClassSessionDto> CancelAsync(int id, CancelClassSessionRequest request, CancellationToken ct = default)
    {
        var session = await uow.ClassSessions.GetWithDetailsAsync(id, ct)
            ?? throw new NotFoundException(nameof(ClassSession), id);

        if (session.Status != ClassSessionStatus.Scheduled)
            throw new ConflictException("Only scheduled sessions can be cancelled.");

        session.Status = ClassSessionStatus.Cancelled;
        session.Notes = AppendNote(session.Notes, "Cancelled", request.Reason);

        uow.ClassSessions.Update(session);
        await uow.SaveChangesAsync(ct);
        return session.ToDto();
    }

    // ---------- helpers ----------

    private async Task EnsureNoConflictsAsync(
        int roomId, int trainerId, DateOnly date, TimeOnly start, TimeOnly end, int? excludeId, CancellationToken ct)
    {
        if (await uow.ClassSessions.HasRoomConflictAsync(roomId, date, start, end, excludeId, ct))
            throw new ConflictException(
                $"The room is already booked on {Fmt(date)} between {Fmt(start)} and {Fmt(end)}.");

        if (await uow.ClassSessions.HasTrainerConflictAsync(trainerId, date, start, end, excludeId, ct))
            throw new ConflictException(
                $"The trainer already has a session on {Fmt(date)} between {Fmt(start)} and {Fmt(end)}.");
    }

    private async Task<Room> GetActiveRoomAsync(int roomId, CancellationToken ct)
    {
        var room = await uow.Rooms.GetByIdAsync(roomId, ct)
            ?? throw new NotFoundException(nameof(Room), roomId);
        if (!room.IsActive)
            throw new ConflictException("The room is inactive.");
        return room;
    }

    private async Task<Trainer> GetActiveTrainerAsync(int trainerId, CancellationToken ct)
    {
        var trainer = await uow.Trainers.GetByIdAsync(trainerId, ct)
            ?? throw new NotFoundException(nameof(Trainer), trainerId);
        if (!trainer.IsActive)
            throw new ConflictException("The trainer is inactive.");
        return trainer;
    }

    private static string? AppendNote(string? notes, string label, string? reason)
    {
        var entry = string.IsNullOrWhiteSpace(reason) ? $"[{label}]" : $"[{label}] {reason.Trim()}";
        return string.IsNullOrWhiteSpace(notes) ? entry : $"{notes}{Environment.NewLine}{entry}";
    }

    private static string Fmt(DateOnly d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    private static string Fmt(TimeOnly t) => t.ToString("HH:mm", CultureInfo.InvariantCulture);
}
