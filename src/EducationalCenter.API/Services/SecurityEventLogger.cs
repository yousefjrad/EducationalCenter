using EducationalCenter.Application.Common.Interfaces;

namespace EducationalCenter.API.Services;

/// <summary>Logs security events with the caller address and user agent. Values are cleaned so they cannot forge log lines.</summary>
public sealed class SecurityEventLogger(ILogger<SecurityEventLogger> logger, IHttpContextAccessor accessor) : ISecurityEventLogger
{
    public void Record(string eventName, int? userId, string? email, string? details)
    {
        var context = accessor.HttpContext;
        var ip = context?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var agent = Clean(context?.Request.Headers.UserAgent.ToString());

        var level = eventName is SecurityEvents.LoginSucceeded or SecurityEvents.PasswordChanged
            ? LogLevel.Information
            : LogLevel.Warning;

        logger.Log(
            level,
            "Security event {SecurityEvent}: user {UserId}, email {Email}, ip {Ip}, agent {UserAgent}, details {Details}",
            eventName, userId, Clean(email), ip, agent, Clean(details));
    }

    private static string? Clean(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var cleaned = value.Replace('\r', ' ').Replace('\n', ' ');
        return cleaned.Length > 200 ? cleaned[..200] : cleaned;
    }
}