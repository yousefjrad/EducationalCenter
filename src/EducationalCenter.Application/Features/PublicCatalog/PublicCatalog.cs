using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Features.Sections;

namespace EducationalCenter.Application.Features.PublicCatalog;

/// <summary>What a visitor sees: no trainer, room or student information.</summary>
public sealed record PublicSectionDto(
    int Id,
    string CourseName,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal Price,
    int SeatsLeft,
    IReadOnlyList<SectionScheduleDto> Schedules);

public interface IPublicCatalogReader
{
    /// <summary>Sections open for enrollment, with the seats still free at <paramref name="utcNow"/>.</summary>
    Task<IReadOnlyList<PublicSectionDto>> GetOpenSectionsAsync(DateTime utcNow, CancellationToken ct = default);
}

public interface IPublicCatalogService
{
    Task<IReadOnlyList<PublicSectionDto>> GetOpenSectionsAsync(CancellationToken ct = default);
}

public sealed class PublicCatalogService(IPublicCatalogReader reader, IClock clock) : IPublicCatalogService
{
    public Task<IReadOnlyList<PublicSectionDto>> GetOpenSectionsAsync(CancellationToken ct = default) =>
        reader.GetOpenSectionsAsync(clock.UtcNow, ct);
}