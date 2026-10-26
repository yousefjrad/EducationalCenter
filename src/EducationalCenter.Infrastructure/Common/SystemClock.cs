using EducationalCenter.Application.Common.Interfaces;

namespace EducationalCenter.Infrastructure.Common;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;

    public DateOnly Today => BusinessTime.DateOf(DateTime.UtcNow);
}
