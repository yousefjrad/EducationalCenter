using EducationalCenter.Application.Common.Models;

namespace EducationalCenter.Application.Features.Sections;

public interface ISectionService
{
    Task<SectionDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PagedResult<SectionDto>> ListAsync(SectionListQuery query, CancellationToken ct = default);
    Task<SectionDto> CreateAsync(CreateSectionRequest request, CancellationToken ct = default);
    Task<SectionDto> UpdateAsync(int id, UpdateSectionRequest request, CancellationToken ct = default);
    Task<SectionDto> ChangeStatusAsync(int id, ChangeSectionStatusRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
