using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Common.Models;
using EducationalCenter.Application.Features.Sections;
using EducationalCenter.Domain.Entities;
using EducationalCenter.Domain.Enums;
using FluentValidation;

namespace EducationalCenter.Application.Features.Imports;

public sealed class SectionImporter(IUnitOfWork uow, IValidator<CreateSectionRequest> validator) : IImporter
{
    public ImportKind Kind => ImportKind.Sections;

    public IReadOnlyList<string> Columns { get; } =
    [
        "CourseCode", "SectionName", "TrainerName", "RoomName", "StartDate", "EndDate",
        "Price", "Capacity", "MinStudents", "Schedule"
    ];

    public ImportTemplate GetTemplate(string language) => ImportTemplates.Sections(language);

    public async Task<ImportOutcome> RunAsync(ImportSheet sheet, bool commit, CancellationToken ct = default)
    {
        var errors = new ImportErrorCollector();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var candidates = new List<Section>();

        // Lookups repeat a lot in a file, so each one is asked of the database only once.
        var courses = new Dictionary<string, Course?>(StringComparer.OrdinalIgnoreCase);
        var trainers = new Dictionary<string, IReadOnlyList<Trainer>>(StringComparer.OrdinalIgnoreCase);
        var rooms = new Dictionary<string, Room?>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in sheet.Rows)
        {
            var before = errors.Count;

            // ----- text lookups -----
            var courseCode = row.Get("CourseCode").ToUpperInvariant();
            Course? course = null;
            if (courseCode.Length == 0)
                errors.Add(row.RowNumber, "CourseCode", "Required.");
            else
            {
                if (!courses.TryGetValue(courseCode, out course))
                {
                    course = await uow.Courses.GetByCodeAsync(courseCode, ct);
                    courses[courseCode] = course;
                }
                if (course is null)
                    errors.Add(row.RowNumber, "CourseCode", $"No course with the code '{courseCode}'.");
                else if (!course.IsActive)
                    errors.Add(row.RowNumber, "CourseCode", "The course is inactive.");
            }

            var trainerName = row.Get("TrainerName");
            Trainer? trainer = null;
            if (trainerName.Length == 0)
                errors.Add(row.RowNumber, "TrainerName", "Required.");
            else
            {
                if (!trainers.TryGetValue(trainerName, out var matches))
                {
                    matches = await uow.Trainers.FindActiveByNameAsync(trainerName, ct);
                    trainers[trainerName] = matches;
                }
                if (matches.Count == 0)
                    errors.Add(row.RowNumber, "TrainerName", $"No active trainer named '{trainerName}'.");
                else if (matches.Count > 1)
                    errors.Add(row.RowNumber, "TrainerName", $"More than one active trainer is named '{trainerName}'.");
                else
                    trainer = matches[0];
            }

            var roomName = row.Get("RoomName");
            Room? room = null;
            if (roomName.Length == 0)
                errors.Add(row.RowNumber, "RoomName", "Required.");
            else
            {
                if (!rooms.TryGetValue(roomName, out room))
                {
                    room = await uow.Rooms.GetByNameAsync(roomName, ct);
                    rooms[roomName] = room;
                }
                if (room is null)
                    errors.Add(row.RowNumber, "RoomName", $"No room named '{roomName}'.");
                else if (!room.IsActive)
                    errors.Add(row.RowNumber, "RoomName", "The room is inactive.");
            }

            // ----- numbers, dates, schedule -----
            if (!ImportParsing.TryParseDate(row.Get("StartDate"), out var startDate))
                errors.Add(row.RowNumber, "StartDate", "Must be a date like 2026-11-01.");
            if (!ImportParsing.TryParseDate(row.Get("EndDate"), out var endDate))
                errors.Add(row.RowNumber, "EndDate", "Must be a date like 2026-12-20.");

            decimal? price = null;
            var priceText = row.Get("Price");
            if (priceText.Length > 0)
            {
                if (ImportParsing.TryParseDecimal(priceText, out var parsedPrice)) price = parsedPrice;
                else errors.Add(row.RowNumber, "Price", "Must be a number (digits and a dot only).");
            }

            if (!ImportParsing.TryParseInt(row.Get("Capacity"), out var capacity))
                errors.Add(row.RowNumber, "Capacity", "Must be a whole number.");

            var minStudents = 0;
            var minText = row.Get("MinStudents");
            if (minText.Length > 0 && !ImportParsing.TryParseInt(minText, out minStudents))
                errors.Add(row.RowNumber, "MinStudents", "Must be a whole number.");

            if (!ImportParsing.TryParseSchedule(row.Get("Schedule"), out var slots, out var scheduleError))
                errors.Add(row.RowNumber, "Schedule", scheduleError);

            var name = row.Get("SectionName");
            if (name.Length == 0)
                errors.Add(row.RowNumber, "SectionName", "Required.");

            if (errors.Count > before)
                continue;

            // ----- shared validation rules (same validator the API uses) -----
            var request = new CreateSectionRequest(
                course!.Id, trainer!.Id, room!.Id, name, startDate, endDate, price, capacity, minStudents, slots);

            var result = await validator.ValidateAsync(request, ct);
            if (!result.IsValid)
            {
                foreach (var failure in result.Errors)
                    errors.Add(row.RowNumber, ColumnFor(failure.PropertyName), failure.ErrorMessage);
                continue;
            }

            // ----- business rules (same as SectionService.CreateAsync) -----
            if (capacity > room.Capacity)
            {
                errors.Add(row.RowNumber, "Capacity", $"Capacity ({capacity}) exceeds the room capacity ({room.Capacity}).");
                continue;
            }

            if (!seen.Add($"{course.Id}|{name}"))
            {
                errors.Add(row.RowNumber, "SectionName", "This course already has a section with this name earlier in the file.");
                continue;
            }

            if (await uow.Sections.NameExistsAsync(course.Id, name, null, ct))
            {
                errors.Add(row.RowNumber, "SectionName", $"This course already has a section named '{name}'.");
                continue;
            }

            candidates.Add(new Section
            {
                CourseId = course.Id,
                TrainerId = trainer.Id,
                RoomId = room.Id,
                Name = name,
                StartDate = startDate,
                EndDate = endDate,
                Price = price ?? course.DefaultPrice,
                Capacity = capacity,
                MinStudents = minStudents,
                Status = SectionStatus.Draft,
                Schedules = slots
                    .Select(s => new SectionSchedule { DayOfWeek = s.DayOfWeek, StartTime = s.StartTime, EndTime = s.EndTime })
                    .ToList()
            });
        }

        var imported = 0;
        if (commit && errors.Count == 0 && candidates.Count > 0)
        {
            await uow.ExecuteInTransactionAsync(async () =>
            {
                await uow.Sections.AddRangeAsync(candidates, ct);
                await uow.SaveChangesAsync(ct);
            }, ct);
            imported = candidates.Count;
        }

        return new ImportOutcome(sheet.Rows.Count, candidates.Count, imported, errors.Errors, errors.Count);
    }

    /// <summary>Maps a request property name back to the spreadsheet column the user must fix.</summary>
    private static string ColumnFor(string property) =>
        property == "Name" ? "SectionName"
        : property.StartsWith("Schedules", StringComparison.Ordinal) ? "Schedule"
        : property;
}
