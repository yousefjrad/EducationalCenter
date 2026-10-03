using EducationalCenter.Application.Common.Models;

namespace EducationalCenter.Application.Features.Enrollments;

public interface IEnrollmentService
{
    Task<EnrollmentDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PagedResult<EnrollmentDto>> ListAsync(EnrollmentListQuery query, CancellationToken ct = default);

    /// <summary>Direct enrollment (Confirmed) or temporary hold (Pending). Fails if the section is full.</summary>
    Task<EnrollmentDto> CreateAsync(CreateEnrollmentRequest request, CancellationToken ct = default);

    /// <summary>Turns a pending hold into a confirmed enrollment.</summary>
    Task<EnrollmentDto> ConfirmAsync(int id, CancellationToken ct = default);

    /// <summary>Frees the seat. Money already paid stays; the open payment plan is cancelled.</summary>
    Task<EnrollmentDto> CancelAsync(int id, CancellationToken ct = default);

    /// <summary>Moves the student (and the payment plan with its payments) to another section of the same course.</summary>
    Task<EnrollmentDto> TransferAsync(int id, TransferEnrollmentRequest request, CancellationToken ct = default);

    /// <summary>Cancels expired holds. Meant to be called periodically by a background job.</summary>
    /// <returns>How many holds were cancelled.</returns>
    Task<int> ExpireHoldsAsync(CancellationToken ct = default);
}
