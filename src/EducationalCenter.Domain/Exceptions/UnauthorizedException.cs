namespace EducationalCenter.Domain.Exceptions;

/// <summary>Wrong credentials or an invalid/expired refresh token. Maps to HTTP 401.</summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message) { }
}
