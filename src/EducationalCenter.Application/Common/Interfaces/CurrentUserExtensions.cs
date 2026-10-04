namespace EducationalCenter.Application.Common.Interfaces;

public static class CurrentUserExtensions
{
    /// <summary>The API layer guarantees authentication; this is a safety net for the services.</summary>
    public static int RequireUserId(this ICurrentUser user) =>
        user.UserId ?? throw new InvalidOperationException("This operation requires an authenticated user.");
}
