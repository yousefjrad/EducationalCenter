namespace EducationalCenter.Application.Common.Interfaces;

/// <summary>Abstracts time so business rules can be unit-tested.</summary>
public interface IClock
{
    DateTime UtcNow { get; }
}
