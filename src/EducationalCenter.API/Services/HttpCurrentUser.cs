using System.Security.Claims;
using EducationalCenter.Application.Common.Interfaces;

namespace EducationalCenter.API.Services;

/// <summary>The signed-in user of the current request, read from the "sub" claim of the access token.</summary>
public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public int? UserId => int.TryParse(Principal?.FindFirst("sub")?.Value, out var id) ? id : null;
}
