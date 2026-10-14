using EducationalCenter.Application.Common.Models;

namespace EducationalCenter.Application.Features.Students;

public interface IStudentService
{
    Task<StudentDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PagedResult<StudentDto>> ListAsync(StudentListQuery query, CancellationToken ct = default);
    Task<StudentDto> CreateAsync(CreateStudentRequest request, CancellationToken ct = default);
    Task<StudentDto> UpdateAsync(int id, UpdateStudentRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Moves the student (and their sign-in account) to another open branch. Refused while the student has a confirmed or
    /// held enrollment or waits in a list. Their past enrollments and payments stay with the branch where they happened.
    /// </summary>
    Task<StudentTransferResultDto> TransferAsync(int id, TransferStudentRequest request, CancellationToken ct = default);
}
