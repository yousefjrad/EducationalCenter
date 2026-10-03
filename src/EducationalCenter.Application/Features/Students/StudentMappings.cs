using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Features.Students;

public static class StudentMappings
{
    public static StudentDto ToDto(this Student s) => new(s.Id, s.FullName, s.PhoneNumber, s.IsActive);
}
