using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EducationalCenter.API.Controllers;

/// <summary>
/// Common to every controller: JSON API behaviour, the /api/v{version}/{controller} route
/// (controller names become kebab-case) and sign-in required by default.
/// A controller adds [ApiVersion("1.0")]; an action adds [HasPermission(...)] or [AllowAnonymous].
/// </summary>
[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class ApiControllerBase : ControllerBase;
