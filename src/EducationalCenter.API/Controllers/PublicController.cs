using Asp.Versioning;
using EducationalCenter.Application.Features.Auth;
using EducationalCenter.Application.Features.PublicCatalog;
using EducationalCenter.Application.Features.Registration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EducationalCenter.API.Controllers;

/// <summary>Open to visitors: the sections that accept enrollment, and self-registration.</summary>
[ApiVersion("1.0")]
[AllowAnonymous]
public sealed class PublicController(IPublicCatalogService catalog, ISelfRegistrationService registration) : ApiControllerBase
{
    [HttpGet("sections")]
    public async Task<ActionResult<IReadOnlyList<PublicSectionDto>>> Sections(CancellationToken ct) =>
        Ok(await catalog.GetOpenSectionsAsync(ct));

    /// <summary>Creates a student with a sign-in account and signs them in.</summary>
    [HttpPost("register")]
    [EnableRateLimiting("login")]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await registration.RegisterAsync(request, ct));
}