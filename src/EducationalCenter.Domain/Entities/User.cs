using EducationalCenter.Domain.Common;

namespace EducationalCenter.Domain.Entities;

public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }

    /// <summary>Consecutive failed sign-ins since the last success or lock. Reset when the account locks or a sign-in succeeds.</summary>
    public int FailedLoginCount { get; set; }

    /// <summary>While this is in the future, sign-in is refused even with the right password.</summary>
    public DateTime? LockedUntil { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
