using EducationalCenter.Application.Common.Models;

namespace EducationalCenter.Application.Features.Trainers;

public interface ITrainerService
{
    Task<TrainerDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PagedResult<TrainerDto>> ListAsync(TrainerListQuery query, CancellationToken ct = default);
    Task<TrainerDto> CreateAsync(CreateTrainerRequest request, CancellationToken ct = default);
    Task<TrainerDto> UpdateAsync(int id, UpdateTrainerRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
