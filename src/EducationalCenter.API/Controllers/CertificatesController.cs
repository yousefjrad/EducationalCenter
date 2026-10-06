using Asp.Versioning;
using EducationalCenter.API.Authorization;
using EducationalCenter.Application.Common.Models;
using EducationalCenter.Application.Features.Certificates;
using EducationalCenter.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace EducationalCenter.API.Controllers;

[ApiVersion("1.0")]
public sealed class CertificatesController(ICertificateService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.Certificates.View)]
    public async Task<ActionResult<PagedResult<CertificateDto>>> List([FromQuery] CertificateListQuery query, CancellationToken ct) =>
        Ok(await service.ListAsync(query, ct));

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.Certificates.View)]
    public async Task<ActionResult<CertificateDto>> GetById(int id, CancellationToken ct) =>
        Ok(await service.GetByIdAsync(id, ct));

    /// <summary>Why a certificate can or cannot be issued now (passing grade and full payment).</summary>
    [HttpGet("enrollments/{enrollmentId:int}/eligibility")]
    [HasPermission(Permissions.Certificates.View)]
    public async Task<ActionResult<CertificateEligibilityDto>> GetEligibility(int enrollmentId, CancellationToken ct) =>
        Ok(await service.GetEligibilityAsync(enrollmentId, ct));

    /// <summary>Normal issue: requires a passing grade and full payment.</summary>
    [HttpPost("enrollments/{enrollmentId:int}/issue")]
    [HasPermission(Permissions.Certificates.Issue)]
    public async Task<ActionResult<CertificateDto>> Issue(int enrollmentId, CancellationToken ct)
    {
        var dto = await service.IssueAsync(enrollmentId, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id, version = "1.0" }, dto);
    }

    /// <summary>Admin-only exception: issues the certificate even if a condition is not met.</summary>
    [HttpPost("enrollments/{enrollmentId:int}/issue-override")]
    [HasPermission(Permissions.Certificates.Override)]
    public async Task<ActionResult<CertificateDto>> IssueWithOverride(int enrollmentId, IssueCertificateOverrideRequest request, CancellationToken ct)
    {
        var dto = await service.IssueWithOverrideAsync(enrollmentId, request, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id, version = "1.0" }, dto);
    }

    /// <param name="language">"ar" or "en": picks that language's default template.</param>
    [HttpGet("{id:int}/pdf")]
    [HasPermission(Permissions.Certificates.View)]
    public async Task<IActionResult> GetPdf(int id, [FromQuery] string language = "ar", CancellationToken ct = default)
    {
        var file = await service.GetPdfAsync(id, language, ct);
        return File(file.Content, "application/pdf", file.FileName);
    }
}
