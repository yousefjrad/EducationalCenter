namespace EducationalCenter.Application.Features.Students;

public sealed record StudentDto(int Id, string FullName, string PhoneNumber, bool IsActive, int BranchId = 0, string? RowVersion = null);

public sealed record CreateStudentRequest(string FullName, string PhoneNumber);

public sealed record UpdateStudentRequest(string FullName, string PhoneNumber, bool IsActive, string? RowVersion = null);

public sealed record StudentListQuery(string? Search = null, bool? IsActive = null, int Page = 1, int PageSize = 20);
