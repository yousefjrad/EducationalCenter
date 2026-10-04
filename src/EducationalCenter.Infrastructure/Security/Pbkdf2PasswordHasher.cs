using System.Security.Cryptography;
using EducationalCenter.Application.Common.Interfaces;

namespace EducationalCenter.Infrastructure.Security;

/// <summary>
/// PBKDF2 with HMAC-SHA256, a random salt per password and 600,000 iterations (the OWASP recommendation),
/// built into .NET, so no extra package is needed. Stored as "PBKDF2-SHA256$iterations$salt$hash".
/// The iteration count is stored with each hash, so it can be raised later without breaking old passwords.
/// </summary>
internal sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const string Prefix = "PBKDF2-SHA256";
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 600_000;
    private const int MaxAcceptedIterations = 10_000_000;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);

        return $"{Prefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
    }

    public bool Verify(string password, string hash)
    {
        var parts = hash.Split('$');

        if (parts.Length != 4
            || parts[0] != Prefix
            || !int.TryParse(parts[1], out var iterations)
            || iterations <= 0
            || iterations > MaxAcceptedIterations)
            return false;

        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expected = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        if (expected.Length == 0)
            return false;

        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);

        // Constant-time comparison, so timing does not reveal how many bytes matched.
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
