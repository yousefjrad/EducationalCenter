namespace EducationalCenter.Infrastructure.Security;

/// <summary>
/// Bound from the "Jwt" configuration section. The secret key is never stored in the repository:
/// set it with "dotnet user-secrets" (development) or an environment variable (production).
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public const int MinimumSecretLength = 32;

    public string Issuer { get; set; } = "EducationalCenter";
    public string Audience { get; set; } = "EducationalCenter.Clients";
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Access tokens are short-lived; the refresh token renews them.</summary>
    public int AccessTokenMinutes { get; set; } = 15;
}
