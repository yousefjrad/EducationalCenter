namespace EducationalCenter.Application.Features.Imports;

public enum ImportKind
{
    Students = 1,
    Courses = 2,
    Sections = 3,
    LegacyPayments = 4
}

public sealed record ImportError(int RowNumber, string Column, string Message);

/// <param name="Committed">True only when the file was saved (commit requested and no errors at all).</param>
/// <param name="Errors">At most the first 500 errors; TotalErrors is the real count.</param>
public sealed record ImportReportDto(
    ImportKind Kind,
    int TotalRows,
    int ValidRows,
    int ImportedRows,
    bool Committed,
    int TotalErrors,
    bool ErrorsTruncated,
    IReadOnlyList<ImportError> Errors);

public sealed record ImportFileDto(byte[] Content, string FileName, string ContentType);

/// <summary>What an importer found. Nothing is saved unless the importer was told to commit and there were no errors.</summary>
public sealed record ImportOutcome(
    int TotalRows,
    int ValidRows,
    int ImportedRows,
    IReadOnlyList<ImportError> Errors,
    int TotalErrors);
