using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Common.Interfaces.Repositories;

public interface IStudentRepository : IRepository<Student>
{
    /// <summary>Same full name AND same phone (family members may legitimately share a phone).</summary>
    Task<bool> ExistsAsync(string fullName, string phoneNumber, int? excludeId, CancellationToken ct = default);

    /// <summary>True if the student has any enrollment or waiting-list entry.</summary>
    Task<bool> HasEnrollmentsAsync(int studentId, CancellationToken ct = default);

    /// <summary>Search by name or phone.</summary>
    Task<(IReadOnlyList<Student> Items, int TotalCount)> SearchAsync(
        string? search, bool? isActive, int page, int pageSize, CancellationToken ct = default);

    /// <summary>The student with exactly that name and phone (name case-insensitive), or null.</summary>
    Task<Student?> FindByNameAndPhoneAsync(string fullName, string phoneNumber, CancellationToken ct = default);
}
