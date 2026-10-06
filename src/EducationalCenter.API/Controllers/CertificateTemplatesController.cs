using Asp.Versioning;
using EducationalCenter.API.Authorization;
using EducationalCenter.Application.Features.CertificateTemplates;
using EducationalCenter.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace EducationalCenter.API.Controllers;

[ApiVersion("1.0")]
public sealed class CertificateTemplatesController(ICertificateTemplateService service) : ApiControllerBase
{
    /// <summary>All templates, or only those of one language ("ar" / "en").</summary>
    [HttpGet]
    [HasPermission(Permissions.CertificateTemplates.View)]
    public async Task<ActionResult<IReadOnlyList<CertificateTemplateDto>>> List([FromQuery] string? language, CancellationToken ct) =>
        Ok(await service.ListAsync(language, ct));

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.CertificateTemplates.View)]
    public async Task<ActionResult<CertificateTemplateDto>> GetById(int id, CancellationToken ct) =>
        Ok(await service.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.CertificateTemplates.Manage)]
    public async Task<ActionResult<CertificateTemplateDto>> Create(CreateCertificateTemplateRequest request, CancellationToken ct)
    {
        var dto = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id, version = "1.0" }, dto);
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.CertificateTemplates.Manage)]
    public async Task<ActionResult<CertificateTemplateDto>> Update(int id, UpdateCertificateTemplateRequest request, CancellationToken ct) =>
        Ok(await service.UpdateAsync(id, request, ct));

    /// <summary>Makes this template the default of its language.</summary>
    [HttpPost("{id:int}/default")]
    [HasPermission(Permissions.CertificateTemplates.Manage)]
    public async Task<ActionResult<CertificateTemplateDto>> SetDefault(int id, CancellationToken ct) =>
        Ok(await service.SetDefaultAsync(id, ct));

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.CertificateTemplates.Manage)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}
