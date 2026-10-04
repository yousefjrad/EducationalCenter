using EducationalCenter.Application.Common.Models;
using EducationalCenter.Application.Features.Enrollments;

namespace EducationalCenter.Application.Features.WaitingList;

public interface IWaitingListService
{
    Task<WaitingListEntryDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PagedResult<WaitingListEntryDto>> ListAsync(WaitingListQuery query, CancellationToken ct = default);

    /// <summary>Only allowed when the section is full; otherwise the student should be enrolled directly.</summary>
    Task<WaitingListEntryDto> AddAsync(AddToWaitingListRequest request, CancellationToken ct = default);

    Task<WaitingListEntryDto> CancelAsync(int id, CancellationToken ct = default);

    /// <summary>Manual promotion by the receptionist. Creates the enrollment and closes the entry.</summary>
    Task<EnrollmentDto> PromoteAsync(int id, PromoteWaitingListEntryRequest request, CancellationToken ct = default);
}
