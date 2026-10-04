using FluentValidation.Results;

namespace EducationalCenter.Application.Common.Exceptions;

/// <summary>Invalid request payload. Maps to HTTP 400.</summary>
public class RequestValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public RequestValidationException(IEnumerable<ValidationFailure> failures)
        : base("One or more validation errors occurred.")
    {
        Errors = failures
            .GroupBy(f => f.PropertyName, f => f.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.ToArray());
    }

    public RequestValidationException(string property, string message) : base(message)
    {
        Errors = new Dictionary<string, string[]> { [property] = new[] { message } };
    }
}
