using Asp.Versioning;
using EducationalCenter.API.Authorization;
using EducationalCenter.Application.Features.Reports;
using EducationalCenter.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace EducationalCenter.API.Controllers;

/// <summary>Reports across all branches. For the super admin only.</summary>
[ApiVersion("1.0")]
public sealed class CrossBranchController(IBranchComparisonService service) : ApiControllerBase
{
    /// <summary>Branches side by side: revenue, expenses, net profit, students, new enrollments, attendance, section fill, collection and overdue.</summary>
    [HttpGet("comparison")]
    [HasPermission(Permissions.Reports.CrossBranch)]
    public async Task<ActionResult<BranchComparisonReportDto>> Comparison([FromQuery] BranchComparisonQuery query, CancellationToken ct) =>
        Ok(await service.GetAsync(query, ct));
}