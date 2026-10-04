namespace EducationalCenter.Domain.Exceptions;

/// <summary>The feature exists in the design but is not installed in this build. Maps to HTTP 501.</summary>
public class FeatureUnavailableException : Exception
{
    public FeatureUnavailableException(string message) : base(message) { }
}
