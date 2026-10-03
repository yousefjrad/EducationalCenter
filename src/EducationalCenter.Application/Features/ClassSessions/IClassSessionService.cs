using EducationalCenter.Application.Common.Models;

namespace EducationalCenter.Application.Features.ClassSessions;

public interface IClassSessionService
{
    Task<ClassSessionDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PagedResult<ClassSessionDto>> ListAsync(ClassSessionListQuery query, CancellationToken ct = default);

    /// <summary>Generates all sessions from the section's weekly schedule (once per section, all-or-nothing).</summary>
    Task<IReadOnlyList<ClassSessionDto>> GenerateForSectionAsync(int sectionId, CancellationToken ct = default);

    Task<ClassSessionDto> CreateAsync(CreateClassSessionRequest request, CancellationToken ct = default);
    Task<ClassSessionDto> UpdateAsync(int id, UpdateClassSessionRequest request, CancellationToken ct = default);

    /// <returns>The replacement session.</returns>
    Task<ClassSessionDto> PostponeAsync(int id, PostponeClassSessionRequest request, CancellationToken ct = default);

    Task<ClassSessionDto> CancelAsync(int id, CancelClassSessionRequest request, CancellationToken ct = default);
}
