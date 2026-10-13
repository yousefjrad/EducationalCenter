using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Domain.Constants;

namespace EducationalCenter.API.Services;

/// <summary>
/// Branch manager: the branch_id claim of the access token, never anything the caller sends.
/// Super Admin: the X-Branch-Id header, or no scope (all branches) when it is absent.
/// </summary>
public sealed class HttpBranchContext(IHttpContextAccessor accessor) : IBranchContext
{
    public const string HeaderName = "X-Branch-Id";

    /// <summary>A signed-in user who belongs to no branch: matches no record.</summary>
    private const int NoBranch = -1;

    private HttpContext? Http => accessor.HttpContext;

    public bool IsSuperAdmin =>
        Http?.User.Identity?.IsAuthenticated == true
        && Http.User.FindFirst("role")?.Value == SystemRoles.Admin;

    public int? BranchId
    {
        get
        {
            var http = Http;
            if (http?.User.Identity?.IsAuthenticated != true)
                return null;

            if (IsSuperAdmin)
                return int.TryParse(http.Request.Headers[HeaderName].ToString(), out var selected) && selected > 0
                    ? selected
                    : null;

            return int.TryParse(http.User.FindFirst("branch_id")?.Value, out var own) && own > 0 ? own : NoBranch;
        }
    }
}