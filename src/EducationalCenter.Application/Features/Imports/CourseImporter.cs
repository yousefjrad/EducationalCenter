using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Common.Models;
using EducationalCenter.Application.Features.Courses;
using EducationalCenter.Domain.Entities;
using FluentValidation;

namespace EducationalCenter.Application.Features.Imports;

public sealed class CourseImporter(IUnitOfWork uow, IValidator<CreateCourseRequest> validator) : IImporter
{
    public ImportKind Kind => ImportKind.Courses;

    public IReadOnlyList<string> Columns { get; } =
        ["Code", "Name", "Description", "Level", "DefaultDurationHours", "DefaultPrice"];

    public ImportTemplate GetTemplate(string language) => ImportTemplates.Courses(language);

    public async Task<ImportOutcome> RunAsync(ImportSheet sheet, bool commit, CancellationToken ct = default)
    {
        var errors = new ImportErrorCollector();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var candidates = new List<Course>();

        foreach (var row in sheet.Rows)
        {
            var before = errors.Count;

            if (!ImportParsing.TryParseInt(row.Get("DefaultDurationHours"), out var hours))
                errors.Add(row.RowNumber, "DefaultDurationHours", "Must be a whole number.");

            if (!ImportParsing.TryParseDecimal(row.Get("DefaultPrice"), out var price))
                errors.Add(row.RowNumber, "DefaultPrice", "Must be a number (digits and a dot only).");

            if (errors.Count > before)
                continue;

            var request = new CreateCourseRequest(
                row.Get("Code"),
                row.Get("Name"),
                NullIfEmpty(row.Get("Description")),
                NullIfEmpty(row.Get("Level")),
                hours,
                price);

            var result = await validator.ValidateAsync(request, ct);
            if (!result.IsValid)
            {
                foreach (var failure in result.Errors)
                    errors.Add(row.RowNumber, failure.PropertyName, failure.ErrorMessage);
                continue;
            }

            var code = request.Code.Trim().ToUpperInvariant();

            if (!seen.Add(code))
            {
                errors.Add(row.RowNumber, "Code", $"The code '{code}' appears earlier in the file.");
                continue;
            }

            if (await uow.Courses.CodeExistsAsync(code, null, ct))
            {
                errors.Add(row.RowNumber, "Code", $"The code '{code}' is already in use.");
                continue;
            }

            candidates.Add(new Course
            {
                Code = code,
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                Level = request.Level?.Trim(),
                DefaultDurationHours = request.DefaultDurationHours,
                DefaultPrice = request.DefaultPrice,
                IsActive = true
            });
        }

        var imported = 0;
        if (commit && errors.Count == 0 && candidates.Count > 0)
        {
            await uow.ExecuteInTransactionAsync(async () =>
            {
                await uow.Courses.AddRangeAsync(candidates, ct);
                await uow.SaveChangesAsync(ct);
            }, ct);
            imported = candidates.Count;
        }

        return new ImportOutcome(sheet.Rows.Count, candidates.Count, imported, errors.Errors, errors.Count);
    }

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
