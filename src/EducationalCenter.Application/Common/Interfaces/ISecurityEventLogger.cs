namespace EducationalCenter.Application.Common.Interfaces;

/// <summary>Writes security-relevant events (sign-ins, lockouts, token reuse) to the application log.</summary>
public interface ISecurityEventLogger
{
    void Record(string eventName, int? userId, string? email, string? details);
}

public static class SecurityEvents
{
    public const string LoginSucceeded = "LoginSucceeded";
    public const string LoginFailed = "LoginFailed";
    public const string LoginBlocked = "LoginBlocked";
    public const string AccountLocked = "AccountLocked";
    public const string RefreshTokenReuse = "RefreshTokenReuse";
    public const string PasswordChanged = "PasswordChanged";
}