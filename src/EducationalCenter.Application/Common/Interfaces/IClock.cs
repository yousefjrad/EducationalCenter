namespace EducationalCenter.Application.Common.Interfaces;

/// <summary>Abstracts time so business rules can be unit-tested.</summary>
public interface IClock
{
    DateTime UtcNow { get; }

    /// <summary>Today's date in the business time zone (Syria), not in UTC.</summary>
    DateOnly Today => DateOnly.FromDateTime(UtcNow);
}
