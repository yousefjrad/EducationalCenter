using EducationalCenter.Application.Common.Exceptions;
using EducationalCenter.Application.Common.Interfaces;

namespace EducationalCenter.Application.Features.Imports;

public sealed class ImportService(IEnumerable<IImporter> importers, IExcelService excel) : IImportService
{
    private const int MaxRows = 5000;
    private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private static readonly string[] Languages = ["ar", "en"];

    public Task<ImportFileDto> GetTemplateAsync(ImportKind kind, string language = "ar", CancellationToken ct = default)
    {
        if (!Languages.Contains(language))
            throw new RequestValidationException("language", "'language' must be 'ar' or 'en'.");

        var importer = Find(kind);
        var content = excel.CreateTemplate(importer.GetTemplate(language));

        return Task.FromResult(new ImportFileDto(content, $"{kind}-template.xlsx", XlsxContentType));
    }

    public Task<ImportReportDto> PreviewAsync(ImportKind kind, Stream file, CancellationToken ct = default) =>
        RunAsync(kind, file, commit: false, ct);

    public Task<ImportReportDto> CommitAsync(ImportKind kind, Stream file, CancellationToken ct = default) =>
        RunAsync(kind, file, commit: true, ct);

    private async Task<ImportReportDto> RunAsync(ImportKind kind, Stream file, bool commit, CancellationToken ct)
    {
        var importer = Find(kind);

        var sheet = excel.ReadSheet(file, importer.Columns);

        if (sheet.Rows.Count == 0)
            throw new RequestValidationException("file", "The file has no data rows.");
        if (sheet.Rows.Count > MaxRows)
            throw new RequestValidationException("file", $"The file has more than {MaxRows} rows. Split it into smaller files.");

        var outcome = await importer.RunAsync(sheet, commit, ct);

        return new ImportReportDto(
            kind,
            outcome.TotalRows,
            outcome.ValidRows,
            outcome.ImportedRows,
            Committed: commit && outcome.TotalErrors == 0 && outcome.ImportedRows > 0,
            outcome.TotalErrors,
            ErrorsTruncated: outcome.TotalErrors > outcome.Errors.Count,
            outcome.Errors);
    }

    private IImporter Find(ImportKind kind) =>
        importers.FirstOrDefault(i => i.Kind == kind)
        ?? throw new RequestValidationException("kind", "Unsupported import kind.");
}
