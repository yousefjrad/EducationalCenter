namespace EducationalCenter.Domain.Exceptions;

/// <summary>Business-rule conflict (room/trainer clash, capacity, duplicate...). Maps to HTTP 409.</summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
