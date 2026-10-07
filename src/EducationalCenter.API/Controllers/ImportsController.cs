using Asp.Versioning;
using EducationalCenter.API.Authorization;
using EducationalCenter.Application.Features.Imports;
using EducationalCenter.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace EducationalCenter.API.Controllers;

[ApiVersion("1.0")]
public sealed class ImportsController(IImportService service) : ApiControllerBase
{
    private const long MaxFileBytes = 10L * 1024 * 1024;

    /// <param name="kind">Students, Courses, Sections or LegacyPayments.</param>
    /// <param name="language">"ar" or "en": the language of the instructions sheet.</param>
    [HttpGet("{kind}/template")]
    [HasPermission(Permissions.Import.Run)]
    public async Task<IActionResult> GetTemplate(ImportKind kind, [FromQuery] string language = "ar", CancellationToken ct = default)
    {
        var file = await service.GetTemplateAsync(kind, language, ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>Checks the whole file and reports every error. Nothing is saved.</summary>
    [HttpPost("{kind}/preview")]
    [HasPermission(Permissions.Import.Run)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxFileBytes)]
    public async Task<ActionResult<ImportReportDto>> Preview(ImportKind kind, IFormFile file, CancellationToken ct)
    {
        var problem = CheckFile(file);
        if (problem is not null) return problem;

        await using var stream = file.OpenReadStream();
        return Ok(await service.PreviewAsync(kind, stream, ct));
    }

    /// <summary>Saves the file only if it has no errors at all (all or nothing); see "committed" in the report.</summary>
    [HttpPost("{kind}/commit")]
    [HasPermission(Permissions.Import.Run)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxFileBytes)]
    public async Task<ActionResult<ImportReportDto>> Commit(ImportKind kind, IFormFile file, CancellationToken ct)
    {
        var problem = CheckFile(file);
        if (problem is not null) return problem;

        await using var stream = file.OpenReadStream();
        return Ok(await service.CommitAsync(kind, stream, ct));
    }

    private ObjectResult? CheckFile(IFormFile? file)
    {
        string? detail = null;

        if (file is null || file.Length == 0)
            detail = "Upload a non-empty .xlsx file in the 'file' field.";
        else if (file.Length > MaxFileBytes)
            detail = "The file is larger than 10 MB.";
        else if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            detail = "Only .xlsx files are accepted.";

        return detail is null
            ? null
            : Problem(detail: detail, statusCode: StatusCodes.Status400BadRequest, title: "Invalid file");
    }
}
