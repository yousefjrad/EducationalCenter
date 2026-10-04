namespace EducationalCenter.Application.Features.Imports;

public interface IImportService
{
    /// <param name="language">"ar" or "en": the language of the instructions sheet.</param>
    Task<ImportFileDto> GetTemplateAsync(ImportKind kind, string language = "ar", CancellationToken ct = default);

    /// <summary>Checks the whole file and reports every error. Nothing is saved.</summary>
    Task<ImportReportDto> PreviewAsync(ImportKind kind, Stream file, CancellationToken ct = default);

    /// <summary>Checks the file again and saves it only if there is not a single error (all or nothing).</summary>
    Task<ImportReportDto> CommitAsync(ImportKind kind, Stream file, CancellationToken ct = default);
}
