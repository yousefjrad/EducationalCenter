using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Common.Models;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Enums;
using EducationalCenter.Domain.Exceptions;

namespace EducationalCenter.Application.Features.Sections;

public sealed class SectionService(IUnitOfWork uow, IClock clock) : ISectionService
{
    private static readonly Dictionary<SectionStatus, SectionStatus[]> AllowedTransitions = new()
    {
        [SectionStatus.Draft] = [SectionStatus.OpenForEnrollment, SectionStatus.Cancelled],
        [SectionStatus.OpenForEnrollment] = [SectionStatus.InProgress, SectionStatus.Cancelled],
        [SectionStatus.InProgress] = [SectionStatus.Completed, SectionStatus.Cancelled],
        [SectionStatus.Completed] = [],
        [SectionStatus.Cancelled] = []
    };

    public async Task<SectionDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var section = await uow.Sections.GetWithDetailsAsync(id, ct)
            ?? throw new NotFoundException(nameof(Section), id);
        return section.ToDto();
    }

    public async Task<PagedResult<SectionDto>> ListAsync(SectionListQuery query, CancellationToken ct = default)
    {
        var (items, total) = await uow.Sections.SearchAsync(
            query.Search?.Trim(), query.CourseId, query.TrainerId, query.RoomId, query.Status,
            query.Page, query.PageSize, ct);

        return new PagedResult<SectionDto>(items.Select(s => s.ToDto()).ToList(), total, query.Page, query.PageSize);
    }

    public async Task<SectionDto> CreateAsync(CreateSectionRequest request, CancellationToken ct = default)
    {
        var course = await uow.Courses.GetByIdAsync(request.CourseId, ct)
            ?? throw new NotFoundException(nameof(Course), request.CourseId);
        if (!course.IsActive)
            throw new ConflictException("The course is inactive.");

        var trainer = await GetActiveTrainerAsync(request.TrainerId, ct);
        var room = await GetActiveRoomAsync(request.RoomId, ct);

        if (request.Capacity > room.Capacity)
            throw new ConflictException($"Capacity ({request.Capacity}) exceeds the room capacity ({room.Capacity}).");

        var name = request.Name.Trim();
        if (await uow.Sections.NameExistsAsync(course.Id, name, null, ct))
            throw new ConflictException($"This course already has a section named '{name}'.");

        var section = new Section
        {
            CourseId = course.Id,
            Course = course,
            TrainerId = trainer.Id,
            Trainer = trainer,
            RoomId = room.Id,
            Room = room,
            Name = name,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Price = request.Price ?? course.DefaultPrice,
            Capacity = request.Capacity,
            MinStudents = request.MinStudents,
            Status = SectionStatus.Draft,
            Schedules = request.Schedules.Select(ToEntity).ToList()
        };

        await uow.Sections.AddAsync(section, ct);
        await uow.SaveChangesAsync(ct);
        return section.ToDto();
    }

    public async Task<SectionDto> UpdateAsync(int id, UpdateSectionRequest request, CancellationToken ct = default)
    {
        var section = await uow.Sections.GetWithDetailsAsync(id, ct)
            ?? throw new NotFoundException(nameof(Section), id);

        if (section.Status is SectionStatus.Completed or SectionStatus.Cancelled)
            throw new ConflictException("A completed or cancelled section cannot be modified.");

        var trainerChanged = request.TrainerId != section.TrainerId;
        var roomChanged = request.RoomId != section.RoomId;

        var trainer = trainerChanged ? await GetActiveTrainerAsync(request.TrainerId, ct) : section.Trainer;
        var room = roomChanged ? await GetActiveRoomAsync(request.RoomId, ct) : section.Room;

        if (request.Capacity > room.Capacity)
            throw new ConflictException($"Capacity ({request.Capacity}) exceeds the room capacity ({room.Capacity}).");

        var seatsTaken = await uow.Enrollments.CountSeatsTakenAsync(id, clock.UtcNow, ct);
        if (request.Capacity < seatsTaken)
            throw new ConflictException($"Capacity cannot be lower than the seats already taken ({seatsTaken}).");

        var name = request.Name.Trim();
        if (!string.Equals(name, section.Name, StringComparison.OrdinalIgnoreCase)
            && await uow.Sections.NameExistsAsync(section.CourseId, name, id, ct))
            throw new ConflictException($"This course already has a section named '{name}'.");

        var schedulesChanged = !SchedulesEqual(section.Schedules, request.Schedules);
        var structuralChange = trainerChanged || roomChanged
            || request.StartDate != section.StartDate
            || request.EndDate != section.EndDate
            || schedulesChanged;

        if (structuralChange
            && (await uow.Sections.HasSessionsAsync(id, ct) || await uow.Sections.HasEnrollmentsAsync(id, ct)))
        {
            throw new ConflictException(
                "Trainer, room, dates and weekly schedule cannot be changed once sessions were generated or students enrolled.");
        }

        section.Name = name;
        section.Price = request.Price;
        section.Capacity = request.Capacity;
        section.MinStudents = request.MinStudents;

        if (structuralChange)
        {
            section.TrainerId = trainer.Id;
            section.Trainer = trainer;
            section.RoomId = room.Id;
            section.Room = room;
            section.StartDate = request.StartDate;
            section.EndDate = request.EndDate;
        }

        if (schedulesChanged)
        {
            uow.Sections.RemoveSchedules(section.Schedules.ToList());
            section.Schedules.Clear();
            foreach (var slot in request.Schedules)
                section.Schedules.Add(ToEntity(slot));
        }

        uow.Sections.Update(section);
        await uow.SaveChangesAsync(ct);
        return section.ToDto();
    }

    public async Task<SectionDto> ChangeStatusAsync(int id, ChangeSectionStatusRequest request, CancellationToken ct = default)
    {
        var section = await uow.Sections.GetWithDetailsAsync(id, ct)
            ?? throw new NotFoundException(nameof(Section), id);

        if (!AllowedTransitions[section.Status].Contains(request.Status))
            throw new ConflictException($"Cannot change a section from {section.Status} to {request.Status}.");

        if (request.Status == SectionStatus.InProgress)
        {
            if (!await uow.Sections.HasSessionsAsync(id, ct))
                throw new ConflictException("Generate the section's sessions before starting it.");
        }
        else if (request.Status == SectionStatus.Cancelled)
        {
            if (await uow.Enrollments.CountSeatsTakenAsync(id, clock.UtcNow, ct) > 0)
                throw new ConflictException("Cancel or transfer the section's enrollments before cancelling the section.");

            var sessions = await uow.ClassSessions.GetBySectionAsync(id, ct);
            foreach (var session in sessions.Where(x => x.Status == ClassSessionStatus.Scheduled))
            {
                session.Status = ClassSessionStatus.Cancelled;
                uow.ClassSessions.Update(session);
            }
        }

        if (request.Status == SectionStatus.Completed)
        {
            // Finishing the section completes its confirmed enrollments.
            var enrolled = await uow.Enrollments.GetEnrolledBySectionAsync(id, ct);
            foreach (var enrollment in enrolled.Where(x => x.Status == EnrollmentStatus.Confirmed))
            {
                enrollment.Status = EnrollmentStatus.Completed;
                uow.Enrollments.Update(enrollment);
            }
        }

        section.Status = request.Status;
        uow.Sections.Update(section);
        await uow.SaveChangesAsync(ct);
        return section.ToDto();
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var section = await uow.Sections.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Section), id);

        if (section.Status != SectionStatus.Draft
            || await uow.Sections.HasSessionsAsync(id, ct)
            || await uow.Sections.HasEnrollmentsAsync(id, ct))
        {
            throw new ConflictException("Only an empty draft section can be deleted. Cancel the section instead.");
        }

        uow.Sections.Remove(section);
        await uow.SaveChangesAsync(ct);
    }

    // ---------- helpers ----------

    private async Task<Trainer> GetActiveTrainerAsync(int trainerId, CancellationToken ct)
    {
        var trainer = await uow.Trainers.GetByIdAsync(trainerId, ct)
            ?? throw new NotFoundException(nameof(Trainer), trainerId);
        if (!trainer.IsActive)
            throw new ConflictException("The trainer is inactive.");
        return trainer;
    }

    private async Task<Room> GetActiveRoomAsync(int roomId, CancellationToken ct)
    {
        var room = await uow.Rooms.GetByIdAsync(roomId, ct)
            ?? throw new NotFoundException(nameof(Room), roomId);
        if (!room.IsActive)
            throw new ConflictException("The room is inactive.");
        return room;
    }

    private static SectionSchedule ToEntity(SectionScheduleDto dto) => new()
    {
        DayOfWeek = dto.DayOfWeek,
        StartTime = dto.StartTime,
        EndTime = dto.EndTime
    };

    private static bool SchedulesEqual(IEnumerable<SectionSchedule> current, IEnumerable<SectionScheduleDto> requested)
    {
        var a = current
            .Select(s => (s.DayOfWeek, s.StartTime, s.EndTime))
            .OrderBy(t => t.DayOfWeek).ThenBy(t => t.StartTime)
            .ToList();
        var b = requested
            .Select(s => (s.DayOfWeek, s.StartTime, s.EndTime))
            .OrderBy(t => t.DayOfWeek).ThenBy(t => t.StartTime)
            .ToList();
        return a.SequenceEqual(b);
    }
}
