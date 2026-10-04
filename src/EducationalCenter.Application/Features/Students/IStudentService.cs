using EducationalCenter.Application.Common.Models;

namespace EducationalCenter.Application.Features.Students;

public interface IStudentService
{
    Task<StudentDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PagedResult<StudentDto>> ListAsync(StudentListQuery query, CancellationToken ct = default);
    Task<StudentDto> CreateAsync(CreateStudentRequest request, CancellationToken ct = default);
    Task<StudentDto> UpdateAsync(int id, UpdateStudentRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
