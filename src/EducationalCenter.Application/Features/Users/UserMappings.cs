using EducationalCenter.Domain.Entities;

namespace EducationalCenter.Application.Features.Users;

public static class UserMappings
{
    /// <remarks>Role must be loaded.</remarks>
    public static UserDto ToDto(this User u) => new(
        u.Id, u.FullName, u.Email, u.RoleId, u.Role.Name, u.IsActive, u.LastLoginAt, u.LockedUntil);
}
