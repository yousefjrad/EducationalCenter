using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Common.Models;
using EducationalCenter.Application.Features.Students;
using EducationalCenter.Domain.Entities;
using FluentValidation;

namespace EducationalCenter.Application.Features.Imports;

public sealed class StudentImporter(IUnitOfWork uow, IValidator<CreateStudentRequest> validator) : IImporter
{
    public ImportKind Kind => ImportKind.Students;
    public IReadOnlyList<string> Columns { get; } = ["FullName", "PhoneNumber"];
    public ImportTemplate GetTemplate(string language) => ImportTemplates.Students(language);

    public async Task<ImportOutcome> RunAsync(ImportSheet sheet, bool commit, CancellationToken ct = default)
    {
        var errors = new ImportErrorCollector();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var candidates = new List<Student>();

        foreach (var row in sheet.Rows)
        {
            var request = new CreateStudentRequest(row.Get("FullName"), row.Get("PhoneNumber"));

            var result = await validator.ValidateAsync(request, ct);
            if (!result.IsValid)
            {
                foreach (var failure in result.Errors)
                    errors.Add(row.RowNumber, failure.PropertyName, failure.ErrorMessage);
                continue;
            }

            var name = request.FullName.Trim();
            var phone = request.PhoneNumber.Trim();

            if (!seen.Add($"{name}|{phone}"))
            {
                errors.Add(row.RowNumber, "FullName", "This student appears earlier in the file.");
                continue;
            }

            if (await uow.Students.ExistsAsync(name, phone, null, ct))
            {
                errors.Add(row.RowNumber, "FullName", "A student with this name and phone already exists.");
                continue;
            }

            candidates.Add(new Student { FullName = name, PhoneNumber = phone, IsActive = true });
        }

        var imported = 0;
        if (commit && errors.Count == 0 && candidates.Count > 0)
        {
            await uow.ExecuteInTransactionAsync(async () =>
            {
                await uow.Students.AddRangeAsync(candidates, ct);
                await uow.SaveChangesAsync(ct);
            }, ct);
            imported = candidates.Count;
        }

        return new ImportOutcome(sheet.Rows.Count, candidates.Count, imported, errors.Errors, errors.Count);
    }
}
