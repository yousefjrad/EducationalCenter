using EducationalCenter.Application.Common.Interfaces;

namespace EducationalCenter.Infrastructure.Common;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
