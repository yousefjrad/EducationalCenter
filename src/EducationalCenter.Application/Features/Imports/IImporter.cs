using EducationalCenter.Application.Common.Models;

namespace EducationalCenter.Application.Features.Imports;

public interface IImporter
{
    ImportKind Kind { get; }

    /// <summary>The column names the uploaded sheet must contain.</summary>
    IReadOnlyList<string> Columns { get; }

    ImportTemplate GetTemplate(string language);

    /// <summary>
    /// Checks every row. When <paramref name="commit"/> is true and there is not a single error,
    /// all rows are saved in one transaction; otherwise nothing is saved (all or nothing).
    /// </summary>
    Task<ImportOutcome> RunAsync(ImportSheet sheet, bool commit, CancellationToken ct = default);
}
